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
    /// <summary>
    /// Ventana para gestionar descuentos por permanencia de clientes con membresía activa.
    /// Permite a administradores y empleados visualizar y modificar descuentos.
    /// </summary>
    public partial class DescuentosPermanenciaWindow : Window
    {
        private readonly ApiService _apiService;
        private ObservableCollection<UsuarioViewModel> _clientes = new();
        private List<UsuarioViewModel> _todosLosClientes = new();
        private UsuarioViewModel? _clienteSeleccionado;

        public DescuentosPermanenciaWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            // Cargar clientes al inicializar
            LoadClientesConMembresia();
        }

        /// <summary>
        /// Carga todos los clientes desde la API (sin restricción de membresía)
        /// </summary>
        private async void LoadClientesConMembresia()
        {
            try
            {
                StatusText.Text = "Cargando clientes...";
                ClientesDataGrid.ItemsSource = _clientes;

                // Llamar a la API para obtener todos los clientes
                var result = await _apiService.GetAllUsersAsync();

                if (result.Success && result.Data != null)
                {
                    var usuarios = result.Data;

                    // Filtrar solo clientes (rol = "cliente"), sin restricción de membresía
                    var clientes = usuarios.Where(u =>
                        u.Rol.Equals("cliente", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(u => u.DescuentoActual ?? 0)
                        .ToList();

                    _todosLosClientes.Clear();
                    foreach (var cliente in clientes)
                    {
                        _todosLosClientes.Add(new UsuarioViewModel(cliente));
                    }

                    AplicarFiltros();
                    StatusText.Text = $"Se cargaron {_todosLosClientes.Count} clientes";
                }
                else
                {
                    MessageBox.Show($"Error al cargar clientes: {result.ErrorMessage}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Error al cargar clientes";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error de conexión";
            }
        }

        /// <summary>
        /// Aplica los filtros de búsqueda
        /// </summary>
        private void AplicarFiltros()
        {
            var clientesFiltrados = _todosLosClientes.AsEnumerable();

            // Filtro por búsqueda (DNI, nombre, email)
            var searchText = SearchTextBox.Text.ToLower();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                clientesFiltrados = clientesFiltrados.Where(c =>
                    c.Dni.ToLower().Contains(searchText) ||
                    c.NombreCompleto.ToLower().Contains(searchText) ||
                    c.Email.ToLower().Contains(searchText));
            }

            _clientes.Clear();
            foreach (var cliente in clientesFiltrados.OrderByDescending(c => c.DescuentoActual ?? 0))
            {
                _clientes.Add(cliente);
            }
        }

        /// <summary>
        /// Evento: Cuando cambia el texto de búsqueda
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        /// <summary>
        /// Evento: Cuando se selecciona un cliente en el DataGrid
        /// </summary>
        private void ClientesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientesDataGrid.SelectedItem is UsuarioViewModel cliente)
            {
                _clienteSeleccionado = cliente;
                MostrarDetallesCliente(cliente);
            }
            else
            {
                LimpiarDetallesCliente();
            }
        }

        /// <summary>
        /// Muestra los detalles del cliente seleccionado en el panel derecho
        /// </summary>
        private void MostrarDetallesCliente(UsuarioViewModel cliente)
        {
            NombreText.Text = cliente.NombreCompleto;
            DniText.Text = cliente.Dni;
            EmailText.Text = cliente.Email;
            TelefonoText.Text = cliente.Telefono ?? "N/A";

            TipoMembresiText.Text = cliente.TipoMembresia;

            // Obtener datos de la membresía desde usuario.membresia
            if (cliente.OriginalUser.Membresia != null && cliente.OriginalUser.Membresia.Activa)
            {
                DateTime? fechaInicio = cliente.OriginalUser.Membresia.FechaInicio;
                DateTime? fechaFin = cliente.OriginalUser.Membresia.FechaFin;

                FechaInicioText.Text = fechaInicio.HasValue ? fechaInicio.Value.ToString("dd/MM/yyyy") : "N/A";
                FechaFinText.Text = fechaFin.HasValue ? fechaFin.Value.ToString("dd/MM/yyyy") : "N/A";
                TiempoRestanteText.Text = cliente.TiempoRestanteMembresia;
            }
            else
            {
                FechaInicioText.Text = "N/A";
                FechaFinText.Text = "N/A";
                TiempoRestanteText.Text = "Sin membresía";
            }

            // Mostrar descuento actual (ya incluye automático + manual)
            double descuentoActual = cliente.OriginalUser.DescuentoActual ?? 0;
            double descuentoAutomatico = cliente.OriginalUser.Membresia?.DescuentoPorPermanencia ?? 0;

            DescuentoActualText.Text = descuentoActual.ToString("F0");

            // El nuevo descuento que se ingresa es el MANUAL, el automático se suma automáticamente en el servidor
            NuevoDescuentoTextBox.Text = "0";
            JustificacionTextBox.Text = "";
        }

        /// <summary>
        /// Limpia el panel de detalles
        /// </summary>
        private void LimpiarDetallesCliente()
        {
            NombreText.Text = "-";
            DniText.Text = "-";
            EmailText.Text = "-";
            TelefonoText.Text = "-";
            TipoMembresiText.Text = "-";
            FechaInicioText.Text = "-";
            FechaFinText.Text = "-";
            TiempoRestanteText.Text = "-";
            DescuentoActualText.Text = "0";
            NuevoDescuentoTextBox.Text = "0";
            JustificacionTextBox.Text = "";
            _clienteSeleccionado = null;
        }

        /// <summary>
        /// Validación: Solo permite números en el campo de descuento
        /// </summary>
        private void NuevoDescuento_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsNumericText(e.Text);
        }

        /// <summary>
        /// Evento: Cuando cambia el valor del descuento
        /// </summary>
        private void NuevoDescuento_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(NuevoDescuentoTextBox.Text))
                return;

            if (int.TryParse(NuevoDescuentoTextBox.Text, out int descuento))
            {
                // Validar rango (0-100%)
                if (descuento < 0)
                    NuevoDescuentoTextBox.Text = "0";
                else if (descuento > 100)
                    NuevoDescuentoTextBox.Text = "100";
            }
        }

        /// <summary>
        /// Valida si el texto contiene solo números
        /// </summary>
        private bool IsNumericText(string text)
        {
            return !string.IsNullOrEmpty(text) && text.All(char.IsDigit);
        }

        /// <summary>
        /// Evento: Guardar cambios de descuento
        /// </summary>
        private async void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteSeleccionado == null)
            {
                MessageBox.Show("Por favor selecciona un cliente", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!double.TryParse(NuevoDescuentoTextBox.Text, out double nuevoDescuento))
            {
                MessageBox.Show("El descuento debe ser un número válido", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (nuevoDescuento < 0 || nuevoDescuento > 100)
            {
                MessageBox.Show("El descuento debe estar entre 0 y 100%", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                GuardarButton.IsEnabled = false;
                GuardarButton.Content = "Guardando...";

                // Actualizar descuento actual usando ApiService
                var motivo = JustificacionTextBox.Text ?? "";
                var result = await _apiService.UpdateDescuentoActualAsync(
                    _clienteSeleccionado.Dni,
                    nuevoDescuento,
                    motivo);

                if (result.Success)
                {
                    MessageBox.Show(
                        $"Descuento actualizado exitosamente\n" +
                        $"Cliente: {_clienteSeleccionado.NombreCompleto}\n" +
                        $"Nuevo descuento: {nuevoDescuento}%",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Actualizar la vista recargando los clientes desde la API
                    LoadClientesConMembresia();
                }
                else
                {
                    MessageBox.Show($"Error al guardar: {result.ErrorMessage}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                GuardarButton.IsEnabled = true;
                GuardarButton.Content = "💾 Guardar Cambios";
            }
        }

        /// <summary>
        /// Evento: Revertir cambios (vuelve al descuento anterior)
        /// </summary>
        private void RevertirButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteSeleccionado == null)
                return;

            var descuentoActual = _clienteSeleccionado.OriginalUser.Membresia?.DescuentoPorPermanencia ?? 0;
            NuevoDescuentoTextBox.Text = descuentoActual.ToString("F0");
            JustificacionTextBox.Text = "";
        }
    }
}
