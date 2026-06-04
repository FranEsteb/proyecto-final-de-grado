using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class NuevoPedidoWindow : Window
    {
        private readonly ApiService _apiService;
        private Proveedor _proveedor;
        private ObservableCollection<LineaPedido> _pedidoProductos;

        public Pedido? PedidoCreado { get; private set; }

        public NuevoPedidoWindow(Proveedor proveedor)
        {
            InitializeComponent();
            _apiService = new ApiService();
            _apiService.UpdateAuthToken();
            _proveedor = proveedor;
            _pedidoProductos = new ObservableCollection<LineaPedido>();

            PedidoListBox.ItemsSource = _pedidoProductos;

            InitializarUI();
        }

        private void InitializarUI()
        {
            // Mostrar información del proveedor
            ProveedorNameTextBlock.Text = _proveedor.Nombre;
            ProveedorCifTextBlock.Text = $"CIF: {_proveedor.CifFormateado}";
            TitleTextBlock.Text = $"Nuevo Pedido - {_proveedor.Nombre}";

            // Cargar productos disponibles del proveedor
            if (_proveedor.ProductosSuministrados != null && _proveedor.ProductosSuministrados.Count > 0)
            {
                foreach (var producto in _proveedor.ProductosSuministrados)
                {
                    ProductosListBox.Items.Add(producto);
                    ProductoComboBox.Items.Add(producto);
                }

                if (ProductoComboBox.Items.Count > 0)
                {
                    ProductoComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                StatusTextBlock.Text = "⚠️ Este proveedor no tiene productos registrados";
                GuardarButton.IsEnabled = false;
            }
        }

        private void AgregarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar selección
                if (ProductoComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Selecciona un producto", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(CantidadTextBox.Text, out int cantidad) || cantidad <= 0)
                {
                    MessageBox.Show("Ingresa una cantidad válida (mayor a 0)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var productoSeleccionado = ProductoComboBox.SelectedItem.ToString();

                // Verificar si el producto ya está en el pedido
                var productoExistente = _pedidoProductos.FirstOrDefault(p => p.Producto == productoSeleccionado);

                if (productoExistente != null)
                {
                    // Actualizar cantidad
                    productoExistente.Cantidad += cantidad;
                }
                else
                {
                    // Agregar nuevo producto
                    _pedidoProductos.Add(new LineaPedido
                    {
                        Producto = productoSeleccionado,
                        Cantidad = cantidad
                    });
                }

                // Limpiar entrada
                CantidadTextBox.Text = "1";
                ProductoComboBox.SelectedIndex = 0;

                ActualizarTotales();
                StatusTextBlock.Text = "✅ Producto agregado";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EliminarProductoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var lineaPedido = button?.DataContext as LineaPedido;

                if (lineaPedido != null)
                {
                    _pedidoProductos.Remove(lineaPedido);
                    ActualizarTotales();
                    StatusTextBlock.Text = "✅ Producto eliminado";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarTotales()
        {
            TotalProductosTextBlock.Text = _pedidoProductos.Count.ToString();
            var cantidadTotal = _pedidoProductos.Sum(p => p.Cantidad);
            CantidadTotalTextBlock.Text = cantidadTotal.ToString();
        }

        private async void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar que haya al menos un producto
                if (_pedidoProductos.Count == 0)
                {
                    MessageBox.Show("Debes agregar al menos un producto al pedido", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                GuardarButton.IsEnabled = false;
                StatusTextBlock.Text = "Creando pedido...";

                // Crear objeto pedido con serialización correcta para el backend
                var numeroPedido = GenerarNumeroPedido();

                // Convertir productos a formato que espera el backend (camelCase)
                var productosFormateados = _pedidoProductos.Select(p => new
                {
                    producto = p.Producto,
                    cantidad = p.Cantidad,
                    precioUnitario = p.PrecioUnitario
                }).ToList();

                // Crear el objeto para enviar al backend
                var pedidoData = new
                {
                    numeroPedido = numeroPedido,
                    proveedorCif = _proveedor.Cif ?? _proveedor.Nombre,
                    proveedorNombre = _proveedor.Nombre,
                    productos = productosFormateados,
                    estado = "pendiente",
                    fechaPedido = DateTime.Now,
                    fechaEntregaEsperada = DateTime.Now.AddDays(7), // Entrega en 7 días por defecto
                    observaciones = !string.IsNullOrEmpty(ObservacionesTextBox.Text) ? ObservacionesTextBox.Text : null
                };

                // Enviar a la API
                var resultado = await _apiService.CreatePedidoAsync(new Pedido
                {
                    NumeroPedido = numeroPedido,
                    ProveedorCif = _proveedor.Cif ?? _proveedor.Nombre,
                    ProveedorNombre = _proveedor.Nombre,
                    Productos = _pedidoProductos.ToList(),
                    Estado = "pendiente",
                    FechaPedido = DateTime.Now,
                    FechaEntregaEsperada = DateTime.Now.AddDays(7),
                    Observaciones = !string.IsNullOrEmpty(ObservacionesTextBox.Text) ? ObservacionesTextBox.Text : null
                });

                if (resultado.Success)
                {
                    PedidoCreado = resultado.Data;
                    StatusTextBlock.Text = "✅ Pedido creado exitosamente";
                    MessageBox.Show($"Pedido {numeroPedido} creado exitosamente",
                                  "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    StatusTextBlock.Text = "❌ Error al crear pedido";
                    MessageBox.Show($"Error: {resultado.ErrorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "❌ Error de conexión";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GuardarButton.IsEnabled = true;
            }
        }

        private string GenerarNumeroPedido()
        {
            // Formato: PED-DDMMYY-HHMMSS
            return $"PED-{DateTime.Now:ddMMyy-HHmmss}";
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
