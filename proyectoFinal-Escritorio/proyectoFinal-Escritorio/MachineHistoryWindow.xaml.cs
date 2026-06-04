using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class MachineHistoryWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly Maquina _machine;

        public MachineHistoryWindow(Maquina machine)
        {
            InitializeComponent();

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            _machine = machine ?? throw new ArgumentNullException(nameof(machine));

            LoadMachineInfo();
            LoadHistoryAsync();
        }

        private void LoadMachineInfo()
        {
            NumeroSerieText.Text = _machine.NumeroSerie;
            TipoModeloText.Text = $"{_machine.TipoCapitalizado} - {_machine.MarcaModelo}";
            MachineInfoText.Text = $"Historial completo de cambios para: {_machine.NumeroSerie}";
        }

        private async void LoadHistoryAsync()
        {
            try
            {
                var result = await _apiService.GetMachineHistoryAsync(_machine.NumeroSerie);

                if (result.Success && result.Data != null)
                {
                    var historyData = result.Data;

                    if (historyData?.historial != null)
                    {
                        var historyList = new List<HistorialEstado>();
                        
                        foreach (var item in historyData.historial)
                        {
                            historyList.Add(new HistorialEstado
                            {
                                Estado = item.estado?.ToString() ?? "",
                                FechaCambio = DateTime.TryParse(item.fechaCambio?.ToString(), out DateTime fecha) ? fecha : DateTime.MinValue,
                                Motivo = item.motivo?.ToString() ?? "Sin motivo especificado",
                                Usuario = item.usuario?.ToString() ?? "Sistema"
                            });
                        }

                        if (historyList.Count > 0)
                        {
                            HistoryItemsControl.ItemsSource = historyList;
                            EmptyStatePanel.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ShowEmptyState();
                        }
                    }
                    else
                    {
                        ShowEmptyState();
                    }
                }
                else
                {
                    MessageBox.Show($"Error al cargar el historial: {result.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowEmptyState();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowEmptyState();
            }
        }

        private void ShowEmptyState()
        {
            HistoryItemsControl.ItemsSource = null;
            EmptyStatePanel.Visibility = Visibility.Visible;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadHistoryAsync();
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

    // Converter para capitalizar strings
    public class StringToTitleCaseConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrEmpty(str))
            {
                return char.ToUpper(str[0]) + str.Substring(1).ToLower();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}