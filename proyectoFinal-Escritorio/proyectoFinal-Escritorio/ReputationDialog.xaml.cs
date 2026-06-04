using System;
using System.Collections.Generic;
using System.Windows;

namespace proyectoFinal_Escritorio
{
    public partial class ReputationDialog : Window
    {
        private readonly List<string> _predefinedReasons;
        
        public string Motivo { get; private set; }

        public ReputationDialog(string title, string message, string type, int points)
        {
            InitializeComponent();
            
            TitleText.Text = title;
            MessageText.Text = message;
            PointsText.Text = $"Puntos: {(points > 0 ? "+" : "")}{points}";
            
            // Configurar colores según el tipo
            if (type == "sancion")
            {
                InfoBorder.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(255, 235, 238)); // Rojo claro
            }
            else if (type == "recompensa")
            {
                InfoBorder.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(232, 245, 233)); // Verde claro
            }
            
            // Configurar motivos predefinidos según el tipo
            _predefinedReasons = GetPredefinedReasons(type);
            PredefinedReasonsPanel.ItemsSource = _predefinedReasons;
        }

        private List<string> GetPredefinedReasons(string type)
        {
            if (type == "sancion")
            {
                return new List<string>
                {
                    "No se presentó a la cita reservada",
                    "Cancelación tardía (menos de 2 horas)",
                    "Comportamiento inadecuado con el personal",
                    "Uso incorrecto del equipo",
                    "Incumplimiento de las normas del gimnasio",
                    "Daño al material o instalaciones"
                };
            }
            else // recompensa
            {
                return new List<string>
                {
                    "Excelente comportamiento y actitud",
                    "Puntualidad constante en las citas",
                    "Cuidado excepcional del equipo",
                    "Ayuda a otros usuarios",
                    "Participación activa en eventos",
                    "Recomendación de nuevos clientes"
                };
            }
        }

        private void PredefinedReasonButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Content is string reason)
            {
                // Si ya hay texto, añadir el motivo predefinido
                if (!string.IsNullOrEmpty(MotivoTextBox.Text))
                {
                    MotivoTextBox.Text += "\n" + reason;
                }
                else
                {
                    MotivoTextBox.Text = reason;
                }
                
                MotivoTextBox.Focus();
                MotivoTextBox.CaretIndex = MotivoTextBox.Text.Length;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var motivo = MotivoTextBox.Text?.Trim();
            
            if (string.IsNullOrEmpty(motivo))
            {
                MessageBox.Show("Por favor, especifica un motivo para esta acción.", 
                              "Motivo requerido", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                MotivoTextBox.Focus();
                return;
            }

            if (motivo.Length < 10)
            {
                MessageBox.Show("El motivo debe tener al menos 10 caracteres para mayor claridad.", 
                              "Motivo muy corto", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                MotivoTextBox.Focus();
                return;
            }

            Motivo = motivo;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            MotivoTextBox.Focus();
        }
    }
}