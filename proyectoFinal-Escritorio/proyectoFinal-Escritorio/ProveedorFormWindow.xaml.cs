using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class ProveedorFormWindow : Window
    {
        private readonly ApiService _apiService;
        private Proveedor? _proveedor;
        private ProveedorFormMode _formMode;

        public Proveedor? Proveedor { get; private set; }

        public ProveedorFormWindow(Proveedor? proveedor, ProveedorFormMode mode)
        {
            InitializeComponent();
            _apiService = new ApiService();
            _apiService.UpdateAuthToken();
            _proveedor = proveedor;
            _formMode = mode;

            SetupFormMode();
            LoadProveedorData();
        }

        private void SetupFormMode()
        {
            switch (_formMode)
            {
                case ProveedorFormMode.Create:
                    TitleTextBlock.Text = "Nuevo Proveedor";
                    GuardarButton.Content = "➕ Crear Proveedor";
                    break;
                case ProveedorFormMode.Edit:
                    TitleTextBlock.Text = "Editar Proveedor";
                    GuardarButton.Content = "💾 Guardar Cambios";
                    break;
                case ProveedorFormMode.View:
                    TitleTextBlock.Text = "Ver Proveedor";
                    GuardarButton.Content = "✏️ Editar";
                    GuardarButton.Click -= GuardarButton_Click;
                    GuardarButton.Click += EditarButton_Click;
                    SetViewMode();
                    break;
            }
        }

        private void LoadProveedorData()
        {
            if (_proveedor != null)
            {
                NombreTextBox.Text = _proveedor.Nombre;
                CifTextBox.Text = _proveedor.Cif ?? string.Empty;
                DireccionTextBox.Text = _proveedor.Direccion ?? string.Empty;
                TelefonoTextBox.Text = _proveedor.Telefono ?? string.Empty;
                EmailTextBox.Text = _proveedor.Email ?? string.Empty;

                if (_proveedor.ProductosSuministrados.Count > 0)
                {
                    ProductosTextBox.Text = string.Join(", ", _proveedor.ProductosSuministrados);
                }

                ObservacionesTextBox.Text = _proveedor.Observaciones ?? string.Empty;
            }
        }

        private void SetViewMode()
        {
            NombreTextBox.IsReadOnly = true;
            CifTextBox.IsReadOnly = true;
            DireccionTextBox.IsReadOnly = true;
            TelefonoTextBox.IsReadOnly = true;
            EmailTextBox.IsReadOnly = true;
            ProductosTextBox.IsReadOnly = true;
            ObservacionesTextBox.IsReadOnly = true;
        }

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_formMode == ProveedorFormMode.View && _proveedor != null)
            {
                // Cambiar a modo edición
                _formMode = ProveedorFormMode.Edit;
                TitleTextBlock.Text = "Editar Proveedor";
                GuardarButton.Content = "💾 Guardar Cambios";
                GuardarButton.Click -= EditarButton_Click;
                GuardarButton.Click += GuardarButton_Click;

                // Habilitar campos
                NombreTextBox.IsReadOnly = false;
                CifTextBox.IsReadOnly = false;
                DireccionTextBox.IsReadOnly = false;
                TelefonoTextBox.IsReadOnly = false;
                EmailTextBox.IsReadOnly = false;
                ProductosTextBox.IsReadOnly = false;
                ObservacionesTextBox.IsReadOnly = false;
            }
        }

        private void LimpiarErrores()
        {
            NombreError.Text = string.Empty;
            CifError.Text = string.Empty;
            DireccionError.Text = string.Empty;
            TelefonoError.Text = string.Empty;
            EmailError.Text = string.Empty;
            ProductosError.Text = string.Empty;
            ObservacionesError.Text = string.Empty;
            GeneralError.Text = string.Empty;
        }

        private void MostrarErrores(List<string> errores)
        {
            LimpiarErrores();

            foreach (var error in errores)
            {
                if (error.Contains("nombre", StringComparison.OrdinalIgnoreCase))
                    NombreError.Text = error;
                else if (error.Contains("CIF", StringComparison.OrdinalIgnoreCase))
                    CifError.Text = error;
                else if (error.Contains("dirección", StringComparison.OrdinalIgnoreCase) ||
                         error.Contains("direccion", StringComparison.OrdinalIgnoreCase))
                    DireccionError.Text = error;
                else if (error.Contains("teléfono", StringComparison.OrdinalIgnoreCase) ||
                         error.Contains("telefono", StringComparison.OrdinalIgnoreCase))
                    TelefonoError.Text = error;
                else if (error.Contains("email", StringComparison.OrdinalIgnoreCase))
                    EmailError.Text = error;
                else if (error.Contains("productos", StringComparison.OrdinalIgnoreCase))
                    ProductosError.Text = error;
                else if (error.Contains("observaciones", StringComparison.OrdinalIgnoreCase))
                    ObservacionesError.Text = error;
                else
                    GeneralError.Text += (string.IsNullOrEmpty(GeneralError.Text) ? "" : "\n") + error;
            }
        }

        private Proveedor ConstruirProveedorDesdeFormulario()
        {
            var productos = new List<string>();
            var productosText = ProductosTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(productosText))
            {
                productos = productosText.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();
            }

            if (_proveedor == null)
            {
                return new Proveedor
                {
                    Nombre = NombreTextBox.Text.Trim(),
                    Cif = CifTextBox.Text.Trim() == string.Empty ? null : CifTextBox.Text.Trim(),
                    Direccion = DireccionTextBox.Text.Trim() == string.Empty ? null : DireccionTextBox.Text.Trim(),
                    Telefono = TelefonoTextBox.Text.Trim() == string.Empty ? null : TelefonoTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim() == string.Empty ? null : EmailTextBox.Text.Trim(),
                    ProductosSuministrados = productos,
                    Observaciones = ObservacionesTextBox.Text.Trim() == string.Empty ? null : ObservacionesTextBox.Text.Trim()
                };
            }
            else
            {
                _proveedor.Nombre = NombreTextBox.Text.Trim();
                _proveedor.Cif = CifTextBox.Text.Trim() == string.Empty ? null : CifTextBox.Text.Trim();
                _proveedor.Direccion = DireccionTextBox.Text.Trim() == string.Empty ? null : DireccionTextBox.Text.Trim();
                _proveedor.Telefono = TelefonoTextBox.Text.Trim() == string.Empty ? null : TelefonoTextBox.Text.Trim();
                _proveedor.Email = EmailTextBox.Text.Trim() == string.Empty ? null : EmailTextBox.Text.Trim();
                _proveedor.ProductosSuministrados = productos;
                _proveedor.Observaciones = ObservacionesTextBox.Text.Trim() == string.Empty ? null : ObservacionesTextBox.Text.Trim();
                _proveedor.UpdatedAt = DateTime.Now;
                return _proveedor;
            }
        }

        private async void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            LimpiarErrores();
            GuardarButton.IsEnabled = false;

            try
            {
                StatusTextBlock.Text = "Validando datos...";

                var proveedor = ConstruirProveedorDesdeFormulario();
                var errores = proveedor.ValidateProveedor();

                if (errores.Count > 0)
                {
                    MostrarErrores(errores);
                    StatusTextBlock.Text = "Errores en el formulario";
                    return;
                }

                if (_formMode == ProveedorFormMode.Create)
                {
                    await GuardarNuevoProveedor(proveedor);
                }
                else if (_formMode == ProveedorFormMode.Edit)
                {
                    await ActualizarProveedor(proveedor);
                }
            }
            catch (Exception ex)
            {
                GeneralError.Text = $"Error: {ex.Message}";
                StatusTextBlock.Text = "Error en el formulario";
            }
            finally
            {
                GuardarButton.IsEnabled = true;
            }
        }

        private async Task GuardarNuevoProveedor(Proveedor proveedor)
        {
            try
            {
                StatusTextBlock.Text = "Creando proveedor...";

                var result = await _apiService.CreateProveedorAsync(proveedor);

                if (result.Success)
                {
                    Proveedor = result.Data;
                    StatusTextBlock.Text = "Proveedor creado correctamente";
                    MessageBox.Show("Proveedor creado exitosamente",
                                  "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    GeneralError.Text = $"Error del servidor: {result.ErrorMessage}";
                    StatusTextBlock.Text = "Error al crear proveedor";
                }
            }
            catch (Exception ex)
            {
                GeneralError.Text = $"Error de conexión: {ex.Message}";
                StatusTextBlock.Text = "Error de conexión";
            }
        }

        private async Task ActualizarProveedor(Proveedor proveedor)
        {
            try
            {
                StatusTextBlock.Text = "Actualizando proveedor...";

                var result = await _apiService.UpdateProveedorAsync(proveedor);

                if (result.Success)
                {
                    Proveedor = result.Data;
                    StatusTextBlock.Text = "Proveedor actualizado correctamente";
                    MessageBox.Show("Proveedor actualizado exitosamente",
                                  "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    GeneralError.Text = $"Error del servidor: {result.ErrorMessage}";
                    StatusTextBlock.Text = "Error al actualizar proveedor";
                }
            }
            catch (Exception ex)
            {
                GeneralError.Text = $"Error de conexión: {ex.Message}";
                StatusTextBlock.Text = "Error de conexión";
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
