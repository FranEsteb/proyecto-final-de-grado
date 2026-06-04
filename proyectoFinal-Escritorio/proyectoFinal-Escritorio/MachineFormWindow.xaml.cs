using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;
using proyectoFinal_Escritorio.Helpers;

namespace proyectoFinal_Escritorio
{
    public partial class MachineFormWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly MachineFormMode _mode;
        private readonly Maquina? _machine;

        public MachineFormWindow(MachineFormMode mode, Maquina? machine)
        {
            InitializeComponent();

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            _mode = mode;
            _machine = machine;

            InitializeForm();
            LoadFormData();
        }

        private void InitializeForm()
        {
            // Configurar título según el modo
            switch (_mode)
            {
                case MachineFormMode.Create:
                    TitleText.Text = "Nueva Máquina";
                    SubtitleText.Text = "Registrar una nueva máquina en el sistema";
                    SaveButton.Content = "Crear Máquina";
                    break;
                case MachineFormMode.Edit:
                    TitleText.Text = "Editar Máquina";
                    SubtitleText.Text = "Modificar información de la máquina";
                    SaveButton.Content = "Guardar Cambios";
                    break;
                case MachineFormMode.View:
                    TitleText.Text = "Detalles de la Máquina";
                    SubtitleText.Text = "Información completa de la máquina";
                    SaveButton.Visibility = Visibility.Collapsed;
                    SetFormReadOnly(true);
                    break;
            }

            // Inicializar ComboBoxes
            TipoComboBox.ItemsSource = Maquina.TiposDisponibles.Select(t => char.ToUpper(t[0]) + t.Substring(1));
            EstadoComboBox.ItemsSource = Maquina.EstadosDisponibles.Select(e => char.ToUpper(e[0]) + e.Substring(1));

            // Cargar proveedores en el ComboBox
            LoadProveedores();
            ProveedorComboBox.SelectionChanged += ProveedorComboBox_SelectionChanged;

            // Valores por defecto
            if (_mode == MachineFormMode.Create)
            {
                TipoComboBox.SelectedIndex = 0;
                EstadoComboBox.SelectedIndex = 0; // "Operativa"
                HorasUsoTextBox.Text = "0";
            }
        }

        private async void LoadProveedores()
        {
            try
            {
                var result = await _apiService.GetAllProveedoresAsync();
                if (result.Success && result.Data != null)
                {
                    // Agregar opción "Sin proveedor"
                    var proveedores = new List<Proveedor>
                    {
                        new Proveedor { Nombre = "Sin proveedor", Id = null, Cif = null }
                    };
                    proveedores.AddRange(result.Data);

                    ProveedorComboBox.ItemsSource = proveedores;
                    ProveedorComboBox.DisplayMemberPath = "Nombre";
                    ProveedorComboBox.SelectedValuePath = "Id"; // Usar Id (ObjectId) en lugar de Cif
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        private void LoadFormData()
        {
            if (_machine != null)
            {
                // Información básica
                NumeroSerieTextBox.Text = _machine.NumeroSerie;
                TipoComboBox.SelectedItem = _machine.TipoCapitalizado;
                MarcaTextBox.Text = _machine.Marca ?? "";
                ModeloTextBox.Text = _machine.Modelo ?? "";
                EstadoComboBox.SelectedItem = _machine.EstadoCapitalizado;
                UbicacionTextBox.Text = _machine.Ubicacion ?? "";
                FechaCompraDatePicker.SelectedDate = _machine.FechaCompra;
                CostoCompraTextBox.Text = _machine.CostoCompra?.ToString("F2") ?? "";

                // Especificaciones técnicas
                PesoTextBox.Text = _machine.Especificaciones?.Peso?.ToString("F1") ?? "";
                DimensionesTextBox.Text = _machine.Especificaciones?.Dimensiones ?? "";
                ConsumoEnergiaTextBox.Text = _machine.Especificaciones?.ConsumoEnergia?.ToString("F0") ?? "";
                CapacidadMaximaTextBox.Text = _machine.Especificaciones?.CapacidadMaxima?.ToString("F0") ?? "";

                // Mantenimiento y uso
                HorasUsoTextBox.Text = _machine.HorasUso.ToString("F1");
                MantenimientoProgramadoDatePicker.SelectedDate = _machine.MantenimientoProgramado;
                UltimoMantenimientoDatePicker.SelectedDate = _machine.UltimoMantenimiento;

                // Garantía
                GarantiaInicioDatePicker.SelectedDate = _machine.Garantia?.FechaInicio;
                GarantiaFinDatePicker.SelectedDate = _machine.Garantia?.FechaFin;
                GarantiaProveedorTextBox.Text = _machine.Garantia?.Proveedor ?? "";

                // Proveedor asignado - Usar el Id (ObjectId) del proveedor
                if (!string.IsNullOrEmpty(_machine.ProveedorId))
                {
                    ProveedorComboBox.SelectedValue = _machine.ProveedorId; // Buscar por ObjectId
                }
                else
                {
                    ProveedorComboBox.SelectedIndex = 0; // "Sin proveedor"
                }

                // Solo lectura en modo Create para número de serie si existe
                if (_mode == MachineFormMode.Edit)
                {
                    NumeroSerieTextBox.IsReadOnly = true;
                }
            }
        }

        private void SetFormReadOnly(bool isReadOnly)
        {
            // Información básica
            NumeroSerieTextBox.IsReadOnly = isReadOnly;
            TipoComboBox.IsEnabled = !isReadOnly;
            MarcaTextBox.IsReadOnly = isReadOnly;
            ModeloTextBox.IsReadOnly = isReadOnly;
            EstadoComboBox.IsEnabled = !isReadOnly;
            UbicacionTextBox.IsReadOnly = isReadOnly;
            FechaCompraDatePicker.IsEnabled = !isReadOnly;
            CostoCompraTextBox.IsReadOnly = isReadOnly;

            // Proveedor
            ProveedorComboBox.IsEnabled = !isReadOnly;

            // Especificaciones técnicas
            PesoTextBox.IsReadOnly = isReadOnly;
            DimensionesTextBox.IsReadOnly = isReadOnly;
            ConsumoEnergiaTextBox.IsReadOnly = isReadOnly;
            CapacidadMaximaTextBox.IsReadOnly = isReadOnly;

            // Mantenimiento y uso
            HorasUsoTextBox.IsReadOnly = isReadOnly;
            MantenimientoProgramadoDatePicker.IsEnabled = !isReadOnly;
            UltimoMantenimientoDatePicker.IsEnabled = !isReadOnly;

            // Garantía
            GarantiaInicioDatePicker.IsEnabled = !isReadOnly;
            GarantiaFinDatePicker.IsEnabled = !isReadOnly;
            GarantiaProveedorTextBox.IsReadOnly = isReadOnly;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                SaveButton.IsEnabled = false;
                SaveButton.Content = "Guardando...";

                var machineData = CreateMachineFromForm();

                if (_mode == MachineFormMode.Create)
                {
                    await CreateMachineAsync(machineData);
                }
                else if (_mode == MachineFormMode.Edit)
                {
                    await UpdateMachineAsync(machineData);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = _mode == MachineFormMode.Create ? "Crear Máquina" : "Guardar Cambios";
            }
        }

        private bool ValidateForm()
        {
            var errors = new StringBuilder();
            string error;

            // Crear objeto Maquina temporal para validación
            var tempMachine = new Maquina
            {
                NumeroSerie = NumeroSerieTextBox.Text?.Trim() ?? "",
                Tipo = TipoComboBox.SelectedItem?.ToString()?.ToLower() ?? "",
                Marca = string.IsNullOrWhiteSpace(MarcaTextBox.Text) ? null : MarcaTextBox.Text.Trim(),
                Modelo = string.IsNullOrWhiteSpace(ModeloTextBox.Text) ? null : ModeloTextBox.Text.Trim(),
                Estado = EstadoComboBox.SelectedItem?.ToString()?.ToLower() ?? "",
                Ubicacion = string.IsNullOrWhiteSpace(UbicacionTextBox.Text) ? null : UbicacionTextBox.Text.Trim(),
                FechaCompra = FechaCompraDatePicker.SelectedDate,
                CostoCompra = string.IsNullOrEmpty(CostoCompraTextBox.Text) ? null :
                    double.TryParse(CostoCompraTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var costo) ? (double?)costo : null,
                HorasUso = double.TryParse(HorasUsoTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var horas) ? horas : 0,
                MantenimientoProgramado = MantenimientoProgramadoDatePicker.SelectedDate,
                UltimoMantenimiento = UltimoMantenimientoDatePicker.SelectedDate
            };

            // Usar el método ValidateMachine del modelo
            var machineErrors = tempMachine.ValidateMachine();
            foreach (var machineError in machineErrors)
            {
                errors.AppendLine(machineError);
            }

            // Validaciones adicionales de campos numéricos opcionales
            if (!string.IsNullOrEmpty(PesoTextBox.Text))
            {
                if (!double.TryParse(PesoTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var peso))
                    errors.AppendLine("• El peso debe ser un número válido");
                else if (peso < 0)
                    errors.AppendLine("• El peso no puede ser negativo");
            }

            if (!string.IsNullOrEmpty(ConsumoEnergiaTextBox.Text))
            {
                if (!double.TryParse(ConsumoEnergiaTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var consumo))
                    errors.AppendLine("• El consumo de energía debe ser un número válido");
                else if (consumo < 0)
                    errors.AppendLine("• El consumo de energía no puede ser negativo");
            }

            if (!string.IsNullOrEmpty(CapacidadMaximaTextBox.Text))
            {
                if (!double.TryParse(CapacidadMaximaTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var capacidad))
                    errors.AppendLine("• La capacidad máxima debe ser un número válido");
                else if (capacidad < 0)
                    errors.AppendLine("• La capacidad máxima no puede ser negativa");
            }

            // Validación de garantía
            if (GarantiaInicioDatePicker.SelectedDate.HasValue && GarantiaFinDatePicker.SelectedDate.HasValue)
            {
                if (!ValidationHelper.ValidateDateRange(
                    GarantiaInicioDatePicker.SelectedDate,
                    GarantiaFinDatePicker.SelectedDate,
                    "Fecha de inicio de garantía",
                    "Fecha de fin de garantía",
                    out error))
                {
                    errors.AppendLine(error);
                }
            }

            // Mostrar errores si los hay
            if (errors.Length > 0)
            {
                MessageBox.Show($"Por favor corrija los siguientes errores:\n\n{errors}",
                              "Errores de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private object CreateMachineFromForm()
        {
            // Obtener el ObjectId del proveedor seleccionado
            var proveedorSeleccionado = ProveedorComboBox.SelectedValue as string;

            var machineData = new
            {
                numeroSerie = NumeroSerieTextBox.Text.Trim(),
                tipo = TipoComboBox.SelectedItem?.ToString()?.ToLower(),
                marca = string.IsNullOrWhiteSpace(MarcaTextBox.Text) ? null : MarcaTextBox.Text.Trim(),
                modelo = string.IsNullOrWhiteSpace(ModeloTextBox.Text) ? null : ModeloTextBox.Text.Trim(),
                estado = EstadoComboBox.SelectedItem?.ToString()?.ToLower(),
                ubicacion = string.IsNullOrWhiteSpace(UbicacionTextBox.Text) ? null : UbicacionTextBox.Text.Trim(),
                proveedor = string.IsNullOrEmpty(proveedorSeleccionado) ? null : proveedorSeleccionado, // ObjectId del proveedor
                fechaCompra = FechaCompraDatePicker.SelectedDate,
                costoCompra = string.IsNullOrEmpty(CostoCompraTextBox.Text) ? (double?)null :
                    double.TryParse(CostoCompraTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var costo) ? (double?)costo : (double?)null,
                horasUso = double.TryParse(HorasUsoTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var horas) ? horas : 0,
                mantenimientoProgramado = MantenimientoProgramadoDatePicker.SelectedDate,
                ultimoMantenimiento = UltimoMantenimientoDatePicker.SelectedDate,
                especificaciones = new
                {
                    peso = string.IsNullOrEmpty(PesoTextBox.Text) ? (double?)null :
                        double.TryParse(PesoTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var peso) ? (double?)peso : (double?)null,
                    dimensiones = string.IsNullOrWhiteSpace(DimensionesTextBox.Text) ? null : DimensionesTextBox.Text.Trim(),
                    consumoEnergia = string.IsNullOrEmpty(ConsumoEnergiaTextBox.Text) ? (double?)null :
                        double.TryParse(ConsumoEnergiaTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var consumo) ? (double?)consumo : (double?)null,
                    capacidadMaxima = string.IsNullOrEmpty(CapacidadMaximaTextBox.Text) ? (double?)null :
                        double.TryParse(CapacidadMaximaTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var capacidad) ? (double?)capacidad : (double?)null
                },
                garantia = new
                {
                    fechaInicio = GarantiaInicioDatePicker.SelectedDate,
                    fechaFin = GarantiaFinDatePicker.SelectedDate,
                    proveedor = string.IsNullOrWhiteSpace(GarantiaProveedorTextBox.Text) ? null : GarantiaProveedorTextBox.Text.Trim()
                }
            };

            return machineData;
        }

        private async Task CreateMachineAsync(object machineData)
        {
            var response = await _apiService.RegisterMachineAsync(machineData);

            if (response.Success)
            {
                MessageBox.Show("Máquina creada exitosamente",
                              "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                var errorData = response.Data;

                var errorMessage = "Error al crear la máquina";
                if (errorData?.errores != null)
                {
                    var errores = new List<string>();
                    foreach (var error in errorData.errores)
                    {
                        errores.Add(error.ToString());
                    }
                    errorMessage += ":\n\n" + string.Join("\n", errores);
                }
                else if (errorData?.message != null)
                {
                    errorMessage += $": {errorData.message}";
                }
                else if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    errorMessage += $": {response.ErrorMessage}";
                }

                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateMachineAsync(object machineData)
        {
            var response = await _apiService.PatchUpdateMachineAsync(machineData);

            if (response.Success)
            {
                MessageBox.Show("Máquina actualizada exitosamente",
                              "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                var errorData = response.Data;

                var errorMessage = "Error al actualizar la máquina";
                if (errorData?.errores != null)
                {
                    var errores = new List<string>();
                    foreach (var error in errorData.errores)
                    {
                        errores.Add(error.ToString());
                    }
                    errorMessage += ":\n\n" + string.Join("\n", errores);
                }
                else if (errorData?.message != null)
                {
                    errorMessage += $": {errorData.message}";
                }
                else if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    errorMessage += $": {response.ErrorMessage}";
                }

                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mode != MachineFormMode.View)
            {
                var result = MessageBox.Show(
                    "¿Estás seguro de que deseas cancelar? Se perderán todos los cambios no guardados.",
                    "Confirmar Cancelación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    DialogResult = false;
                    Close();
                }
            }
            else
            {
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }
}