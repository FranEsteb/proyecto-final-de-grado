using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class ReputationHistoryWindow : Window, INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private readonly string _usuarioDni;
        private ObservableCollection<ReputationHistoryViewModel> _historyItems = new();

        public ObservableCollection<ReputationHistoryViewModel> HistoryItems
        {
            get => _historyItems;
            set
            {
                _historyItems = value;
                OnPropertyChanged(nameof(HistoryItems));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReputationHistoryWindow(string usuarioDni)
        {
            InitializeComponent();
            DataContext = this;

            _usuarioDni = usuarioDni;
            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            LoadUserInfoAsync();
            LoadHistoryAsync();
        }

        private async void LoadUserInfoAsync()
        {
            try
            {
                var response = await _apiService.GetAllUsersAsync();

                if (response.Success)
                {
                    var users = response.Data ?? new List<Usuario>();

                    var user = users.FirstOrDefault(u => u.Dni == _usuarioDni);
                    if (user != null)
                    {
                        ClienteText.Text = user.NombreCompleto;
                        DniText.Text = user.Dni;
                        ReputacionActualText.Text = user.Reputacion.ToString();

                        SubtitleText.Text = $"Historial de {user.NombreCompleto}";

                        // Actualizar color de reputación
                        if (user.Reputacion >= 80)
                        {
                            ReputacionActualText.Foreground = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(16, 124, 16)); // Verde
                        }
                        else if (user.Reputacion < 50)
                        {
                            ReputacionActualText.Foreground = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(209, 52, 56)); // Rojo
                        }
                        else
                        {
                            ReputacionActualText.Foreground = new System.Windows.Media.SolidColorBrush(
                                System.Windows.Media.Color.FromRgb(255, 140, 0)); // Naranja
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"Error al cargar información del usuario: {response.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar información del usuario: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadHistoryAsync()
        {
            try
            {
                StatusText.Text = "Cargando historial...";

                var response = await _apiService.GetReputacionHistoricalAsync(_usuarioDni);

                if (response.Success && !string.IsNullOrEmpty(response.Data))
                {
                    try
                    {
                        var historyData = JsonConvert.DeserializeObject<ReputationHistoryResponse>(response.Data);

                        if (historyData?.Registros != null && historyData.Registros.Count > 0)
                        {
                            HistoryItems.Clear();

                            // Ordenar la lista antes de procesarla
                            var sortedRegistros = historyData.Registros
                                .OrderByDescending(r => r.Fecha)
                                .ToList();

                            foreach (var item in sortedRegistros)
                            {
                                HistoryItems.Add(new ReputationHistoryViewModel(item));
                            }

                            HistoryDataGrid.ItemsSource = HistoryItems;
                            TotalRegistrosText.Text = HistoryItems.Count.ToString();

                            StatusText.Text = "Historial cargado";
                            LastUpdateText.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";
                        }
                        else
                        {
                            StatusText.Text = "Sin registros de historial";
                            TotalRegistrosText.Text = "0";
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al procesar historial: {ex.Message}",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusText.Text = "Error al procesar datos";
                    }
                }
                else
                {
                    MessageBox.Show($"Error al cargar historial: {response.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Error al cargar historial";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error de conexión";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUserInfoAsync();
            LoadHistoryAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }

    // ViewModels para el historial
    public class ReputationHistoryViewModel
    {
        public ReputationHistoryItem OriginalItem { get; }

        public ReputationHistoryViewModel(ReputationHistoryItem item)
        {
            OriginalItem = item ?? throw new ArgumentNullException(nameof(item));
        }

        public DateTime Fecha => OriginalItem.Fecha;
        public string FechaFormateada => OriginalItem.Fecha.ToString("dd/MM/yyyy HH:mm");
        public string Tipo => OriginalItem.Tipo;
        public string Motivo => OriginalItem.Motivo;
        public int Puntos => OriginalItem.Puntos;
        public bool EsPositivo => Puntos > 0;
        public string PuntosFormateados => Puntos > 0 ? $"+{Puntos}" : Puntos.ToString();
    }

    // Modelos para deserializar la respuesta del API
    public class ReputationHistoryResponse
    {
        public List<ReputationHistoryItem> Registros { get; set; } = new();
        public ReputationSummary Resumen { get; set; } = new();
    }

    public class ReputationHistoryItem
    {
        public string IdRep { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = "";
        public string Motivo { get; set; } = "";
        public int Puntos { get; set; }
    }

    public class ReputationSummary
    {
        public int TotalRegistros { get; set; }
        public int TotalRecompensas { get; set; }
        public int TotalSanciones { get; set; }
        public int PuntosGanados { get; set; }
        public int PuntosPerdidos { get; set; }
        public DateTime? UltimaActividad { get; set; }
    }
}