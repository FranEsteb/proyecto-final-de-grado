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
    public partial class ProveedoresWindow : Window
    {
        private readonly ApiService _apiService;
        private ObservableCollection<ProveedorViewModel> allProveedores;
        private ObservableCollection<ProveedorViewModel> filteredProveedores;
        private ObservableCollection<PedidoViewModel> allPedidos;
        private ObservableCollection<PedidoViewModel> filteredPedidos;

        // Referencia estática a la ventana abierta actualmente
        public static ProveedoresWindow? CurrentInstance { get; private set; }

        public ProveedoresWindow()
        {
            InitializeComponent();

            // Verificar permisos de acceso
            if (!PermissionHelper.CanAccessAdminSection())
            {
                MessageBox.Show("❌ No tienes permisos para acceder a la gestión de proveedores. Solo administradores pueden acceder.",
                              "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Loaded += (s, e) => this.Close();
                return;
            }

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();
            allProveedores = new ObservableCollection<ProveedorViewModel>();
            filteredProveedores = new ObservableCollection<ProveedorViewModel>();
            allPedidos = new ObservableCollection<PedidoViewModel>();
            filteredPedidos = new ObservableCollection<PedidoViewModel>();

            ProveedoresDataGrid.ItemsSource = filteredProveedores;
            PedidosDataGrid.ItemsSource = filteredPedidos;

            // Registrar esta instancia como la actualmente abierta
            CurrentInstance = this;

            LoadProveedores();
            LoadPedidos();
        }

        private async void LoadProveedores()
        {
            try
            {
                SetLoadingState(true);
                StatusTextBlock.Text = "Cargando proveedores...";

                var result = await _apiService.GetAllProveedoresAsync();

                if (result.Success && result.Data != null)
                {
                    var proveedores = result.Data;

                    allProveedores.Clear();
                    foreach (var proveedor in proveedores)
                    {
                        allProveedores.Add(new ProveedorViewModel(proveedor));
                    }

                    ApplyCurrentFilters();

                    StatusTextBlock.Text = "Proveedores cargados correctamente";
                    TotalProveedoresText.Text = $"Total: {allProveedores.Count} proveedores";
                }
                else
                {
                    StatusTextBlock.Text = "Error al cargar proveedores";
                    MessageBox.Show($"Error al conectar con el servidor: {result.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Error de conexión";
                MessageBox.Show($"Error al cargar proveedores: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void ApplyCurrentFilters()
        {
            var filtered = allProveedores.AsEnumerable();

            // Filtro por nombre
            var nombreFilter = BuscarNombreTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(nombreFilter))
            {
                filtered = filtered.Where(p => p.Nombre.Contains(nombreFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por CIF
            var cifFilter = BuscarCifTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(cifFilter))
            {
                filtered = filtered.Where(p => !string.IsNullOrEmpty(p.Cif) &&
                                             p.Cif.Contains(cifFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por producto
            var productoFilter = BuscarProductoTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(productoFilter))
            {
                filtered = filtered.Where(p => p.ProductosSuministrados.Any(prod =>
                                             prod.Contains(productoFilter, StringComparison.OrdinalIgnoreCase)));
            }

            filteredProveedores.Clear();
            foreach (var proveedor in filtered)
            {
                filteredProveedores.Add(proveedor);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            NuevoProveedorButton.IsEnabled = !isLoading;
            ProveedoresDataGrid.IsEnabled = !isLoading;
        }

        // ====== EVENTOS DE BOTONES ======

        private void NuevoProveedorButton_Click(object sender, RoutedEventArgs e)
        {
            var formWindow = new ProveedorFormWindow(null, ProveedorFormMode.Create);
            formWindow.ShowDialog();

            // Recargar la lista después de crear un nuevo proveedor
            if (formWindow.Proveedor != null)
            {
                LoadProveedores();
            }
        }

        private void PedirButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var proveedor = button?.DataContext as ProveedorViewModel;

            if (proveedor != null)
            {
                var nuevoPedidoWindow = new NuevoPedidoWindow(proveedor.OriginalProveedor);
                nuevoPedidoWindow.ShowDialog();

                // Si se creó un pedido, mostrar mensaje de éxito
                if (nuevoPedidoWindow.PedidoCreado != null)
                {
                    StatusTextBlock.Text = $"✅ Pedido {nuevoPedidoWindow.PedidoCreado.NumeroPedido} creado exitosamente";
                }
            }
        }

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var proveedor = button?.DataContext as ProveedorViewModel;

            if (proveedor != null)
            {
                var formWindow = new ProveedorFormWindow(proveedor.OriginalProveedor, ProveedorFormMode.Edit);
                formWindow.ShowDialog();

                // Recargar la lista después de editar
                if (formWindow.Proveedor != null)
                {
                    LoadProveedores();
                }
            }
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var proveedor = button?.DataContext as ProveedorViewModel;

            if (proveedor != null)
            {
                var result = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar a {proveedor.Nombre}?",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        SetLoadingState(true);
                        StatusTextBlock.Text = "Eliminando proveedor...";

                        // Usar CIF para eliminar, no ID
                        var cif = proveedor.Cif ?? proveedor.Nombre;
                        var deleteResult = await _apiService.DeleteProveedorAsync(cif);

                        if (deleteResult.Success)
                        {
                            StatusTextBlock.Text = "Proveedor eliminado correctamente";
                            LoadProveedores();
                        }
                        else
                        {
                            StatusTextBlock.Text = "Error al eliminar proveedor";
                            MessageBox.Show($"Error: {deleteResult.ErrorMessage}",
                                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusTextBlock.Text = "Error de conexión";
                        MessageBox.Show($"Error: {ex.Message}",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        SetLoadingState(false);
                    }
                }
            }
        }

        private void BuscarButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyCurrentFilters();
        }

        private void LimpiarFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            BuscarNombreTextBox.Clear();
            BuscarCifTextBox.Clear();
            BuscarProductoTextBox.Clear();
            ApplyCurrentFilters();
        }

        private void ActualizarButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProveedores();
        }

        private void VolverButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ProveedoresDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Doble clic en una fila abre el formulario de edición
            if (ProveedoresDataGrid.SelectedItem is ProveedorViewModel proveedor)
            {
                var formWindow = new ProveedorFormWindow(proveedor.OriginalProveedor, ProveedorFormMode.View);
                formWindow.ShowDialog();
            }
        }

        // ====== PEDIDOS TAB METHODS ======

        private async void LoadPedidos()
        {
            try
            {
                StatusPedidosTextBlock.Text = "Cargando pedidos...";

                var result = await _apiService.GetAllPedidosAsync();

                if (result.Success && result.Data != null)
                {
                    var pedidos = result.Data;

                    allPedidos.Clear();
                    foreach (var pedido in pedidos)
                    {
                        allPedidos.Add(new PedidoViewModel(pedido));
                    }

                    ApplyPedidoFilters();
                    StatusPedidosTextBlock.Text = $"✅ {allPedidos.Count} pedidos cargados";
                }
                else
                {
                    StatusPedidosTextBlock.Text = "Error al cargar pedidos";
                    MessageBox.Show($"Error al conectar con el servidor: {result.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusPedidosTextBlock.Text = "Error de conexión";
                MessageBox.Show($"Error al cargar pedidos: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyPedidoFilters()
        {
            var filtered = allPedidos.AsEnumerable();

            // Filtro por proveedor
            var proveedorFilter = FiltroProveedorPedidosTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(proveedorFilter))
            {
                filtered = filtered.Where(p => p.ProveedorNombre.Contains(proveedorFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por estado
            if (FiltroEstadoPedidosComboBox.SelectedIndex > 0) // 0 es "Todos los estados"
            {
                var estadoFilter = (FiltroEstadoPedidosComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                if (!string.IsNullOrEmpty(estadoFilter))
                {
                    filtered = filtered.Where(p => p.EstadoCapitalizado.Equals(estadoFilter, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Filtro por número de pedido
            var numeroFilter = BuscarNumeroPedidoTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(numeroFilter))
            {
                filtered = filtered.Where(p => p.NumeroPedido.Contains(numeroFilter, StringComparison.OrdinalIgnoreCase));
            }

            filteredPedidos.Clear();
            foreach (var pedido in filtered)
            {
                filteredPedidos.Add(pedido);
            }
        }

        private void BuscarPedidosButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyPedidoFilters();
        }

        private void ActualizarPedidosButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPedidos();
        }

        private void VerPedidoButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var pedido = button?.DataContext as PedidoViewModel;

            if (pedido != null)
            {
                // Construir la lista de productos
                var sb = new StringBuilder();
                sb.AppendLine($"Número de Pedido: {pedido.NumeroPedido}");
                sb.AppendLine($"Proveedor: {pedido.ProveedorNombre}");
                sb.AppendLine($"Estado: {pedido.EstadoCapitalizado}");
                sb.AppendLine($"Fecha Pedido: {pedido.FechaPedidoFormateada}");
                sb.AppendLine($"Entrega Esperada: {pedido.FechaEntregaEsperadaFormateada}");
                sb.AppendLine();
                sb.AppendLine("📦 Productos:");
                sb.AppendLine("─────────────────────────");

                // Mostrar cada producto con su cantidad
                foreach (var producto in pedido.OriginalPedido.Productos)
                {
                    sb.AppendLine($"  • x{producto.Cantidad} {producto.Producto}");
                }

                sb.AppendLine("─────────────────────────");
                sb.AppendLine($"Total de productos: {pedido.CantidadTotal}");

                if (!string.IsNullOrEmpty(pedido.Observaciones))
                {
                    sb.AppendLine();
                    sb.AppendLine($"Observaciones: {pedido.Observaciones}");
                }

                MessageBox.Show(sb.ToString(), "Detalles del Pedido", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void MarcarEntregadoButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var pedido = button?.DataContext as PedidoViewModel;

            if (pedido != null)
            {
                // No permitir marcar como entregado si ya está entregado
                if (pedido.Estado == "entregado")
                {
                    MessageBox.Show("Este pedido ya está marcado como entregado",
                                  "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"¿Deseas marcar el pedido {pedido.NumeroPedido} como entregado?",
                    "Confirmar Entrega",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusPedidosTextBlock.Text = "Actualizando estado del pedido...";

                        // Crear una copia del pedido con el estado actualizado
                        var pedidoActualizado = new Pedido
                        {
                            NumeroPedido = pedido.NumeroPedido,
                            ProveedorCif = pedido.ProveedorCif,
                            ProveedorNombre = pedido.ProveedorNombre,
                            Productos = pedido.OriginalPedido.Productos,
                            Estado = "entregado",
                            FechaPedido = pedido.FechaPedido,
                            FechaEntregaEsperada = pedido.FechaEntregaEsperada,
                            FechaEntrega = DateTime.Now, // Registrar fecha de entrega actual
                            Observaciones = pedido.Observaciones
                        };

                        var updateResult = await _apiService.UpdatePedidoAsync(pedidoActualizado);

                        if (updateResult.Success)
                        {
                            StatusPedidosTextBlock.Text = "✅ Pedido marcado como entregado";
                            LoadPedidos(); // Recargar la lista
                        }
                        else
                        {
                            StatusPedidosTextBlock.Text = "❌ Error al actualizar pedido";
                            MessageBox.Show($"Error: {updateResult.ErrorMessage}",
                                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusPedidosTextBlock.Text = "❌ Error de conexión";
                        MessageBox.Show($"Error: {ex.Message}",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void EliminarPedidoButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var pedido = button?.DataContext as PedidoViewModel;

            if (pedido != null)
            {
                var result = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar el pedido {pedido.NumeroPedido}?",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusPedidosTextBlock.Text = "Eliminando pedido...";

                        var deleteResult = await _apiService.DeletePedidoAsync(pedido.NumeroPedido);

                        if (deleteResult.Success)
                        {
                            StatusPedidosTextBlock.Text = "✅ Pedido eliminado correctamente";
                            LoadPedidos();
                        }
                        else
                        {
                            StatusPedidosTextBlock.Text = "❌ Error al eliminar pedido";
                            MessageBox.Show($"Error: {deleteResult.ErrorMessage}",
                                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusPedidosTextBlock.Text = "❌ Error de conexión";
                        MessageBox.Show($"Error: {ex.Message}",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void PedidosDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Doble clic en una fila muestra los detalles del pedido
            if (PedidosDataGrid.SelectedItem is PedidoViewModel pedido)
            {
                // Construir la lista de productos
                var sb = new StringBuilder();
                sb.AppendLine($"Número de Pedido: {pedido.NumeroPedido}");
                sb.AppendLine($"Proveedor: {pedido.ProveedorNombre}");
                sb.AppendLine($"Estado: {pedido.EstadoCapitalizado}");
                sb.AppendLine($"Fecha Pedido: {pedido.FechaPedidoFormateada}");
                sb.AppendLine($"Entrega Esperada: {pedido.FechaEntregaEsperadaFormateada}");
                sb.AppendLine();
                sb.AppendLine("📦 Productos:");
                sb.AppendLine("─────────────────────────");

                // Mostrar cada producto con su cantidad
                foreach (var producto in pedido.OriginalPedido.Productos)
                {
                    sb.AppendLine($"  • x{producto.Cantidad} {producto.Producto}");
                }

                sb.AppendLine("─────────────────────────");
                sb.AppendLine($"Total de productos: {pedido.CantidadTotal}");

                if (!string.IsNullOrEmpty(pedido.Observaciones))
                {
                    sb.AppendLine();
                    sb.AppendLine($"Observaciones: {pedido.Observaciones}");
                }

                MessageBox.Show(sb.ToString(), "Detalles del Pedido", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
