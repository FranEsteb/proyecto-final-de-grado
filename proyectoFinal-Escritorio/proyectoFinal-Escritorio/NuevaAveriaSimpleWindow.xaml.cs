using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;
using proyectoFinal_Escritorio.Helpers;

namespace proyectoFinal_Escritorio
{
    // Ventana simple para crear nuevas averías
    // He diseñado esta clase para ser directa y fácil de usar
    public partial class NuevaAveriaSimpleWindow : Window
    {
        private readonly ApiService _apiService;

        public bool DialogResultValue { get; private set; }

        public NuevaAveriaSimpleWindow()
        {
            InitializeComponent();

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            SetupPlaceholders();
        }

        // Configuro los placeholders en los campos de texto
        private void SetupPlaceholders()
        {
            SetPlaceholder(ElementoAfectadoTextBox);
            SetPlaceholder(DescripcionTextBox);
            SetPlaceholder(ObservacionesTextBox);
        }

        // Establezco placeholder en un TextBox
        private void SetPlaceholder(TextBox textBox)
        {
            if (string.IsNullOrEmpty(textBox.Text) && !string.IsNullOrEmpty(textBox.Tag?.ToString()))
            {
                textBox.Text = textBox.Tag.ToString();
                textBox.Foreground = System.Windows.Media.Brushes.Gray;
                
                textBox.GotFocus += (s, e) => {
                    if (textBox.Foreground == System.Windows.Media.Brushes.Gray)
                    {
                        textBox.Text = "";
                        textBox.Foreground = System.Windows.Media.Brushes.Black;
                    }
                };
                
                textBox.LostFocus += (s, e) => {
                    if (string.IsNullOrEmpty(textBox.Text))
                    {
                        SetPlaceholder(textBox);
                    }
                };
            }
        }

        // Valido que los campos obligatorios estén completos
        private bool ValidateForm()
        {
            var errors = new StringBuilder();
            string error;

            // Elemento afectado
            var elementoAfectado = ElementoAfectadoTextBox.Text;
            if (string.IsNullOrEmpty(elementoAfectado) || elementoAfectado == ElementoAfectadoTextBox.Tag?.ToString())
            {
                elementoAfectado = "";
            }

            if (!ValidationHelper.ValidateRequired(elementoAfectado, "El elemento afectado", out error))
                errors.AppendLine(error);

            // Descripción
            var descripcion = DescripcionTextBox.Text;
            if (string.IsNullOrEmpty(descripcion) || descripcion == DescripcionTextBox.Tag?.ToString())
            {
                descripcion = "";
            }

            if (!ValidationHelper.ValidateRequired(descripcion, "La descripción del problema", out error))
                errors.AppendLine(error);

            // Prioridad
            if (!ValidationHelper.ValidateComboBoxSelection(PrioridadComboBox.SelectedItem, "una prioridad", out error))
                errors.AppendLine(error);

            // Costo de reparación (obligatorio y positivo)
            decimal costo;
            if (!ValidationHelper.ValidateNumericPositive(CostoReparacionTextBox.Text, "El costo de reparación", out costo, out error))
                errors.AppendLine(error);

            if (errors.Length > 0)
            {
                MessageBox.Show($"Por favor corrija los siguientes errores:\n\n{errors}",
                              "Errores de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // Creo el objeto Averia con los datos del formulario
        private Averia CreateAveriaFromForm()
        {
            var selectedItem = PrioridadComboBox.SelectedItem as ComboBoxItem;
            var prioridad = selectedItem?.Tag?.ToString() ?? "media";

            // El costo ya está validado, así que podemos parsearlo directamente
            decimal.TryParse(CostoReparacionTextBox.Text, out decimal costo);

            var averia = new Averia
            {
                ElementoAfectado = ElementoAfectadoTextBox.Text,
                Descripcion = DescripcionTextBox.Text,
                Prioridad = prioridad,
                Estado = "pendiente",
                FechaReporte = DateTime.Now,
                CostoReparacion = costo, // Ahora siempre se incluye porque es obligatorio
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Observaciones si están especificadas
            if (!string.IsNullOrEmpty(ObservacionesTextBox.Text) &&
                ObservacionesTextBox.Text != ObservacionesTextBox.Tag?.ToString())
            {
                averia.Observaciones = ObservacionesTextBox.Text;
            }

            return averia;
        }

        // Guardo la nueva avería en la API
        private async Task<bool> SaveAveriaAsync()
        {
            try
            {
                var averia = CreateAveriaFromForm();

                var result = await _apiService.CreateAveriaAsync(averia);

                if (result.Success)
                {
                    MessageBox.Show($"Avería reportada exitosamente:\n\n" +
                                  $"Elemento: {averia.ElementoAfectado}\n" +
                                  $"Prioridad: {averia.PrioridadCapitalizada}",
                                  "Avería Creada", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show($"Error al crear la avería: {result.ErrorMessage}", "Error",
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
            DialogResultValue = false;
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            SaveButton.IsEnabled = false;
            SaveButton.Content = "Guardando...";

            try
            {
                var success = await SaveAveriaAsync();
                if (success)
                {
                    DialogResultValue = true;
                    Close();
                }
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = "Reportar Avería";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }
}