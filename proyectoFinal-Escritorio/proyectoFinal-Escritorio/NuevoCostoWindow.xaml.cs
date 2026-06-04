using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    // Ventana para registrar nuevos gastos de reparación y mantenimiento
    public partial class NuevoCostoWindow : Window
    {
        private readonly ApiService _apiService;
        private List<Tecnico> _tecnicos = new();
        private List<Proveedor> _proveedores = new();
        private readonly Maquina? _machine;

        public NuevoCostoWindow()
        {
            InitializeComponent();

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            // Configurar fecha por defecto (hoy)
            FechaDatePicker.SelectedDate = DateTime.Now;

            // Cargar técnicos y proveedores disponibles
            LoadTecnicosAsync();
            LoadProveedoresAsync();
        }

        public NuevoCostoWindow(Maquina machine) : this()
        {
            _machine = machine;
            LoadMachineInfo();
        }

        private void LoadMachineInfo()
        {
            if (_machine != null)
            {
                // Actualizar el título y descripción con información de la máquina
                Title = $"Registrar Costo - {_machine.NumeroSerie}";

                // Actualizar la descripción sugerida
                DescripcionTextBox.Text = $"Reparación completada para máquina {_machine.NumeroSerie} ({_machine.TipoCapitalizado} - {_machine.MarcaModelo})";
            }
        }

        // Método para obtener el ObjectId de MongoDB de la máquina
        private async Task<string?> GetMachineObjectIdAsync(string numeroSerie)
        {
            try
            {
                var result = await _apiService.GetMachineByIdAsync(numeroSerie);

                if (result.Success && result.Data != null)
                {
                    return result.Data?._id?.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener ID de máquina: {ex.Message}");
            }

            return null;
        }

        // Cargar técnicos desde la API
        private async void LoadTecnicosAsync()
        {
            try
            {
                var result = await _apiService.GetActiveTechnicosAsync();

                if (result.Success && result.Data != null)
                {
                    _tecnicos = JsonConvert.DeserializeObject<List<Tecnico>>(result.Data.ToString()) ?? new List<Tecnico>();

                    // Limpiar y agregar técnicos al ComboBox
                    TecnicoComboBox.Items.Clear();

                    // Agregar opción "Sin asignar"
                    var itemSinAsignar = new ComboBoxItem { Content = "(Sin asignar)" };
                    TecnicoComboBox.Items.Add(itemSinAsignar);

                    // Agregar técnicos
                    foreach (var tecnico in _tecnicos)
                    {
                        var item = new ComboBoxItem
                        {
                            Content = $"{tecnico.NombreCompleto} - {tecnico.Especialidad}",
                            Tag = tecnico.Id
                        };
                        TecnicoComboBox.Items.Add(item);
                    }

                    TecnicoComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                // Si falla la carga de técnicos, continuar sin ellos
                System.Diagnostics.Debug.WriteLine($"Error al cargar técnicos: {ex.Message}");
            }
        }

        // Cargar proveedores desde la API
        private async void LoadProveedoresAsync()
        {
            try
            {
                var result = await _apiService.GetAllProveedoresAsync();
                if (result.Success && result.Data != null)
                {
                    // Agregar opción "Sin proveedor"
                    _proveedores = new List<Proveedor>
                    {
                        new Proveedor { Nombre = "Sin proveedor", Id = null, Cif = null }
                    };
                    _proveedores.AddRange(result.Data);

                    ProveedorComboBox.ItemsSource = _proveedores;
                    ProveedorComboBox.DisplayMemberPath = "Nombre";
                    ProveedorComboBox.SelectedValuePath = "Id";
                    ProveedorComboBox.SelectionChanged += ProveedorComboBox_SelectionChanged;
                    ProveedorComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar proveedores: {ex.Message}");
            }
        }

        // Mostrar detalles del proveedor seleccionado
        private void ProveedorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var proveedorSeleccionado = ProveedorComboBox.SelectedItem as Proveedor;
            if (proveedorSeleccionado != null)
            {
                var descripcion = "";
                if (!string.IsNullOrEmpty(proveedorSeleccionado.Cif))
                {
                    descripcion = $"CIF: {proveedorSeleccionado.CifFormateado}\n";
                    descripcion += $"Email: {proveedorSeleccionado.EmailFormateado}\n";
                    descripcion += $"Teléfono: {proveedorSeleccionado.TelefonoFormateado}";
                }
                else
                {
                    descripcion = "No hay proveedor asignado";
                }
                ProveedorDescripcionTextBlock.Text = descripcion;
            }
        }

        // Valido que los campos obligatorios estén completos
        private bool ValidateForm()
        {
            // Tipo de costo
            if (TipoCostoComboBox.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un tipo de costo",
                              "Campo obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
                TipoCostoComboBox.Focus();
                return false;
            }

            // Monto
            if (string.IsNullOrWhiteSpace(MontoTextBox.Text))
            {
                MessageBox.Show("Debe especificar el monto del costo",
                              "Campo obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
                MontoTextBox.Focus();
                return false;
            }

            if (!decimal.TryParse(MontoTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal monto))
            {
                MessageBox.Show("El monto debe ser un número válido",
                              "Formato incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
                MontoTextBox.Focus();
                return false;
            }

            if (monto <= 0)
            {
                MessageBox.Show("El monto debe ser mayor a cero",
                              "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                MontoTextBox.Focus();
                return false;
            }

            // Fecha
            if (!FechaDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Debe seleccionar una fecha",
                              "Campo obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
                FechaDatePicker.Focus();
                return false;
            }

            if (FechaDatePicker.SelectedDate.Value > DateTime.Now)
            {
                MessageBox.Show("La fecha no puede ser futura",
                              "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                FechaDatePicker.Focus();
                return false;
            }

            // Descripción
            if (string.IsNullOrWhiteSpace(DescripcionTextBox.Text))
            {
                MessageBox.Show("Debe proporcionar una descripción del costo",
                              "Campo obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
                DescripcionTextBox.Focus();
                return false;
            }

            return true;
        }

        // Creo el objeto Costo con los datos del formulario
        private Costo CreateCostoFromForm()
        {
            var selectedTipo = TipoCostoComboBox.SelectedItem as ComboBoxItem;
            var tipo = selectedTipo?.Tag?.ToString() ?? "Reparacion";

            var monto = decimal.Parse(MontoTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture);

            var costo = new Costo
            {
                TipoCosto = tipo,
                Monto = monto,
                Fecha = FechaDatePicker.SelectedDate!.Value,
                Descripcion = DescripcionTextBox.Text.Trim(),
                UsuarioRegistro = SessionManager.GetCurrentUsername() ?? "Administrador"
            };

            // Si hay máquina asociada, establecer su ID
            if (_machine != null)
            {
                costo.MaquinaId = _machine.NumeroSerie;
            }

            // Campos opcionales
            // Obtener el ObjectId del proveedor seleccionado
            var proveedorSeleccionado = ProveedorComboBox.SelectedValue as string;
            if (!string.IsNullOrEmpty(proveedorSeleccionado))
            {
                costo.ProveedorId = proveedorSeleccionado;
            }

            if (TecnicoComboBox.SelectedItem is ComboBoxItem selectedTecnico && selectedTecnico.Tag != null)
            {
                costo.TecnicoId = selectedTecnico.Tag.ToString();
            }

            if (!string.IsNullOrWhiteSpace(NumeroFacturaTextBox.Text))
            {
                costo.NumeroFactura = NumeroFacturaTextBox.Text.Trim();
            }

            if (!string.IsNullOrWhiteSpace(ObservacionesTextBox.Text))
            {
                costo.Observaciones = ObservacionesTextBox.Text.Trim();
            }

            return costo;
        }

        // Guardo el nuevo costo en la API
        private async Task<bool> SaveCostoAsync()
        {
            try
            {
                var costo = CreateCostoFromForm();

                // Si hay máquina asociada, obtener su ObjectId de MongoDB
                string? maquinaObjectId = null;
                if (_machine != null)
                {
                    maquinaObjectId = await GetMachineObjectIdAsync(_machine.NumeroSerie);
                    if (string.IsNullOrEmpty(maquinaObjectId))
                    {
                        MessageBox.Show("No se pudo obtener el ID de la máquina. Por favor, intente nuevamente.",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }

                // Crear objeto con el formato correcto para la API
                var costoData = new
                {
                    tipoCosto = costo.TipoCosto,
                    monto = costo.Monto,
                    fecha = costo.Fecha,
                    descripcion = costo.Descripcion,
                    maquinaId = maquinaObjectId,
                    proveedorId = costo.ProveedorId,
                    tecnicoId = costo.TecnicoId,
                    numeroFactura = costo.NumeroFactura,
                    observaciones = costo.Observaciones,
                    usuarioRegistro = costo.UsuarioRegistro
                };

                // Usar ApiService para crear el costo
                var result = await _apiService.CreateCostoAsync(costoData);

                if (result.Success)
                {
                    MessageBox.Show($"Costo registrado exitosamente:\n\n" +
                                  $"Tipo: {costo.TipoCostoCapitalizado}\n" +
                                  $"Monto: {costo.MontoFormateado}\n" +
                                  $"Descripción: {costo.Descripcion}",
                                  "Costo Registrado", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show($"Error al guardar el costo: {result.ErrorMessage}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Eventos de los botones
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            SaveButton.IsEnabled = false;
            SaveButton.Content = "Guardando...";

            try
            {
                var success = await SaveCostoAsync();
                if (success)
                {
                    DialogResult = true;
                    Close();
                }
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = "Guardar Costo";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }
}
