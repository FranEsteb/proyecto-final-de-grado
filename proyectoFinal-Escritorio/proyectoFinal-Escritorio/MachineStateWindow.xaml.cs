using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class MachineStateWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly Maquina _machine;

        public MachineStateWindow(Maquina machine)
        {
            InitializeComponent();

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            _machine = machine ?? throw new ArgumentNullException(nameof(machine));

            LoadMachineInfo();
            InitializeEstadoOptions();
        }

        private void LoadMachineInfo()
        {
            // Mostrar información de la máquina
            NumeroSerieText.Text = _machine.NumeroSerie;
            TipoModeloText.Text = $"{_machine.TipoCapitalizado} - {_machine.MarcaModelo}";
            EstadoActualText.Text = _machine.EstadoCapitalizado;
            
            MachineInfoText.Text = $"Cambiar estado de: {_machine.NumeroSerie}";

            // Color del indicador de estado actual
            SetEstadoColor(EstadoActualEllipse, _machine.Estado);
        }

        private void InitializeEstadoOptions()
        {
            // Llenar ComboBox con estados disponibles
            var estados = Maquina.EstadosDisponibles.Select(e => new { 
                Texto = char.ToUpper(e[0]) + e.Substring(1),
                Valor = e
            }).ToList();

            NuevoEstadoComboBox.ItemsSource = estados;
            NuevoEstadoComboBox.DisplayMemberPath = "Texto";
            NuevoEstadoComboBox.SelectedValuePath = "Valor";

            // Seleccionar el estado actual por defecto
            NuevoEstadoComboBox.SelectedValue = _machine.Estado;
        }

        private void SetEstadoColor(System.Windows.Shapes.Ellipse ellipse, string estado)
        {
            var color = estado.ToLower() switch
            {
                "operativa" => Colors.Green,
                "en reparación" => Colors.Red,
                "fuera de servicio" => Colors.DarkRed,
                "mantenimiento" => Colors.Orange,
                _ => Colors.Gray
            };

            ellipse.Fill = new SolidColorBrush(color);
        }

        private void NuevoEstadoComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (NuevoEstadoComboBox.SelectedValue is string nuevoEstado)
            {
                // Mostrar panel de horas si el nuevo estado es operativa
                if (nuevoEstado == "operativa")
                {
                    HorasUsoPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    HorasUsoPanel.Visibility = Visibility.Collapsed;
                }

                // Actualizar el texto informativo según el estado
                UpdateInfoText(nuevoEstado);

                // Proporcionar sugerencias de motivo
                SuggestMotive(nuevoEstado);
            }
        }

        private void UpdateInfoText(string nuevoEstado)
        {
            var infoTexts = new Dictionary<string, string>
            {
                ["operativa"] = "La máquina estará disponible para uso por los clientes. Se solicitarán los gastos de reparación y mantenimiento. Puedes registrar horas adicionales de uso si es necesario.",
                ["en reparación"] = "La máquina no estará disponible hasta que se complete la reparación.",
                ["fuera de servicio"] = "La máquina será marcada como no operativa de forma permanente hasta nueva actualización.",
                ["mantenimiento"] = "La máquina estará temporalmente fuera de servicio para tareas de mantenimiento preventivo."
            };

            if (infoTexts.ContainsKey(nuevoEstado))
            {
                InfoText.Text = $"💡 {infoTexts[nuevoEstado]} Esta acción quedará registrada en el historial de la máquina.";
            }
        }

        private void SuggestMotive(string nuevoEstado)
        {
            // Solo sugerir si el campo está vacío
            if (string.IsNullOrEmpty(MotivoTextBox.Text))
            {
                var suggestions = new Dictionary<string, string>
                {
                    ["operativa"] = "Reparación completada, máquina lista para uso",
                    ["en reparación"] = "Máquina presenta fallas que requieren atención técnica",
                    ["fuera de servicio"] = "Equipo dañado, requiere reemplazo o reparación mayor",
                    ["mantenimiento"] = "Mantenimiento preventivo programado"
                };

                if (suggestions.ContainsKey(nuevoEstado))
                {
                    MotivoTextBox.Text = suggestions[nuevoEstado];
                    // Seleccionar el texto para que el usuario pueda editarlo fácilmente
                    MotivoTextBox.Focus();
                    MotivoTextBox.SelectAll();
                }
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                UpdateButton.IsEnabled = false;
                UpdateButton.Content = "Actualizando...";

                var nuevoEstado = NuevoEstadoComboBox.SelectedValue?.ToString();
                var motivo = MotivoTextBox.Text.Trim();

                // Si el nuevo estado es operativa, mostrar ventana de registro de gastos
                if (nuevoEstado == "operativa")
                {
                    var costsWindow = new NuevoCostoWindow(_machine);
                    if (costsWindow.ShowDialog() != true)
                    {
                        // Usuario canceló la ventana de gastos
                        UpdateButton.IsEnabled = true;
                        UpdateButton.Content = "Actualizar Estado";
                        return;
                    }
                }

                // Actualizar estado sin gastos (ahora los gastos se registran independientemente)
                await UpdateMachineStateAsync(nuevoEstado, motivo);

                // Si se especificaron horas de uso adicionales, registrarlas
                if (HorasUsoPanel.Visibility == Visibility.Visible &&
                    !string.IsNullOrEmpty(HorasUsoTextBox.Text) &&
                    double.TryParse(HorasUsoTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var horasAdicionales) &&
                    horasAdicionales > 0)
                {
                    await RegisterAdditionalUsageAsync(horasAdicionales);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar el estado: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateButton.IsEnabled = true;
                UpdateButton.Content = "Actualizar Estado";
            }
        }

        private bool ValidateForm()
        {
            var errors = new List<string>();

            // Validar estado seleccionado
            if (NuevoEstadoComboBox.SelectedValue == null)
            {
                errors.Add("Debe seleccionar un estado");
            }

            // Validar motivo
            if (string.IsNullOrWhiteSpace(MotivoTextBox.Text))
            {
                errors.Add("Debe proporcionar un motivo para el cambio");
            }
            else if (MotivoTextBox.Text.Trim().Length < 10)
            {
                errors.Add("El motivo debe tener al menos 10 caracteres");
            }

            // Validar horas de uso si está visible
            if (HorasUsoPanel.Visibility == Visibility.Visible && 
                !string.IsNullOrEmpty(HorasUsoTextBox.Text))
            {
                if (!double.TryParse(HorasUsoTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var horas))
                {
                    errors.Add("Las horas de uso deben ser un número válido");
                }
                else if (horas < 0 || horas > 24)
                {
                    errors.Add("Las horas de uso deben estar entre 0 y 24");
                }
            }

            // Mostrar errores si los hay
            if (errors.Any())
            {
                var errorMessage = "Por favor corrige los siguientes errores:\n\n" + string.Join("\n", errors);
                MessageBox.Show(errorMessage, "Errores de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private async Task UpdateMachineStateAsync(string nuevoEstado, string motivo)
        {
            var requestData = new
            {
                numeroSerie = _machine.NumeroSerie,
                estado = nuevoEstado,
                motivo = motivo
            };

            var response = await _apiService.UpdateMachineStateAsync(requestData);

            if (response.Success)
            {
                var mensaje = $"Estado actualizado exitosamente a '{char.ToUpper(nuevoEstado[0]) + nuevoEstado.Substring(1)}'";

                MessageBox.Show(mensaje,
                              "Actualización Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var errorMessage = "Error al actualizar el estado";
                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    errorMessage += $": {response.ErrorMessage}";
                }

                throw new Exception(errorMessage);
            }
        }

        private async Task RegisterAdditionalUsageAsync(double horasAdicionales)
        {
            var requestData = new
            {
                numeroSerie = _machine.NumeroSerie,
                horasUso = horasAdicionales
            };

            var response = await _apiService.RegisterMachineUsageAsync(requestData);

            if (response.Success)
            {
                MessageBox.Show($"Se registraron {horasAdicionales:F1} horas adicionales de uso",
                              "Horas Registradas", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // No lanzar excepción aquí ya que el estado ya se actualizó
                MessageBox.Show($"El estado se actualizó correctamente, pero hubo un error al registrar las horas de uso: {response.ErrorMessage}",
                              "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DialogResult != false)
            {
                DialogResult = true;
            }
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }
}