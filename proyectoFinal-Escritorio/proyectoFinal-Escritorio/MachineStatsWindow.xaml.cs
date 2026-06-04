using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class MachineStatsWindow : Window
    {
        private readonly ApiService _apiService;

        public class TipoStat
        {
            public string Tipo { get; set; } = "";
            public int Cantidad { get; set; }
        }

        public MachineStatsWindow()
        {
            InitializeComponent();

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            LoadStatsAsync();
        }

        private async void LoadStatsAsync()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;

                var result = await _apiService.GetMachineStatisticsAsync();

                if (result.Success && result.Data != null)
                {
                    var stats = result.Data;

                    if (stats != null)
                    {
                        // Estadísticas generales
                        TotalMaquinasText.Text = stats.total?.ToString() ?? "0";
                        OperativasText.Text = stats.estados?.operativas?.ToString() ?? "0";
                        EnReparacionText.Text = stats.estados?.enReparacion?.ToString() ?? "0";
                        MantenimientoText.Text = stats.estados?.mantenimiento?.ToString() ?? "0";

                        // Estadísticas por tipo
                        var tipoStats = new List<TipoStat>();
                        if (stats.porTipo != null)
                        {
                            foreach (var tipo in stats.porTipo)
                            {
                                var tipoNombre = tipo._id?.ToString() ?? "Unknown";
                                // Capitalizar primera letra
                                tipoNombre = char.ToUpper(tipoNombre[0]) + tipoNombre.Substring(1);
                                
                                tipoStats.Add(new TipoStat
                                {
                                    Tipo = tipoNombre,
                                    Cantidad = (int)(tipo.cantidad ?? 0)
                                });
                            }
                        }

                        TipoStatsListBox.ItemsSource = tipoStats;
                        LoadingPanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        ShowError("No se pudieron cargar las estadísticas");
                    }
                }
                else
                {
                    ShowError($"Error del servidor: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error de conexión: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            LoadingPanel.Visibility = Visibility.Visible;
            
            // Actualizar el panel de loading para mostrar error
            var loadingStack = LoadingPanel.Child as StackPanel;
            if (loadingStack != null && loadingStack.Children.Count >= 2)
            {
                if (loadingStack.Children[0] is TextBlock iconText)
                    iconText.Text = "❌";
                
                if (loadingStack.Children[1] is TextBlock messageText)
                    messageText.Text = message;
            }

            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatsAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }
}