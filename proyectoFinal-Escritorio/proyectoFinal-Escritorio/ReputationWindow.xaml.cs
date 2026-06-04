using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class ReputationWindow : Window, INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private ObservableCollection<ReputationUserViewModel> _clients = new();
        private ObservableCollection<ReputationUserViewModel> _filteredClients = new();
        private string _searchText = "";

        public ObservableCollection<ReputationUserViewModel> FilteredClients
        {
            get => _filteredClients;
            set
            {
                _filteredClients = value;
                OnPropertyChanged(nameof(FilteredClients));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterClients();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReputationWindow()
        {
            InitializeComponent();
            DataContext = this;

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            LoadClientsAsync();
        }


        private async void LoadClientsAsync()
        {
            try
            {
                StatusText.Text = "Cargando clientes...";

                var response = await _apiService.GetAllUsersAsync();

                if (response.Success)
                {
                    var users = response.Data ?? new List<Usuario>();

                    // Filtrar solo clientes
                    var clients = users.Where(u => u.Rol.Equals("cliente", StringComparison.OrdinalIgnoreCase)).ToList();

                    _clients.Clear();
                    foreach (var client in clients)
                    {
                        _clients.Add(new ReputationUserViewModel(client));
                    }

                    FilterClients();
                    UpdateStatistics();
                    UpdateStatus();
                }
                else
                {
                    MessageBox.Show($"Error al cargar los clientes: {response.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Error al cargar datos";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error de conexión";
            }
        }

        private void FilterClients()
        {
            // Mostrar todos los clientes sin filtro
            FilteredClients = new ObservableCollection<ReputationUserViewModel>(_clients.OrderByDescending(c => c.Reputacion));
            ClientsDataGrid.ItemsSource = FilteredClients;

            UpdateClientCount();
        }

        private void UpdateClientCount()
        {
            var total = _clients.Count;
            var filtered = FilteredClients.Count;
            
            if (filtered == total)
            {
                ClientCountText.Text = $"{total} clientes registrados";
            }
            else
            {
                ClientCountText.Text = $"{filtered} de {total} clientes mostrados";
            }
        }

        private void UpdateStatistics()
        {
            var high = _clients.Count(c => c.EsReputacionAlta);
            var medium = _clients.Count(c => c.EsReputacionMedia);
            var low = _clients.Count(c => c.EsReputacionBaja);
            var eligible = _clients.Count(c => c.PuedeRecibirPremio);

            HighReputationText.Text = high.ToString();
            MediumReputationText.Text = medium.ToString();
            LowReputationText.Text = low.ToString();
            EligibleForRewardText.Text = eligible.ToString();
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";
            LastUpdateText.Text = $"{_clients.Count} clientes cargados";
        }

        // Event Handlers
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadClientsAsync();
        }

        private async void SancionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ReputationUserViewModel client)
            {
                var motivo = await ShowReputationDialog("Aplicar Sanción", 
                    $"¿Por qué motivo deseas sancionar a {client.NombreCompleto}?", 
                    "sancion", -5);
                
                if (!string.IsNullOrEmpty(motivo))
                {
                    await ApplyReputationChange(client, "sancion", motivo, -5);
                }
            }
        }

        private async void RecompensaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ReputationUserViewModel client)
            {
                var motivo = await ShowReputationDialog("Aplicar Recompensa", 
                    $"¿Por qué motivo deseas recompensar a {client.NombreCompleto}?", 
                    "recompensa", 5);
                
                if (!string.IsNullOrEmpty(motivo))
                {
                    await ApplyReputationChange(client, "recompensa", motivo, 5);
                }
            }
        }

        private async void PremioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ReputationUserViewModel client)
            {
                var result = MessageBox.Show(
                    $"¿Deseas otorgar un premio especial a {client.NombreCompleto}?\n" +
                    $"Reputación actual: {client.Reputacion} puntos\n" +
                    $"Premio: +20 puntos y reconocimiento especial",
                    "Premio Especial",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await ApplyReputationChange(client, "recompensa", "Premio especial por excelente comportamiento", 20);
                }
            }
        }

        private void HistorialButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ReputationUserViewModel client)
            {
                var historyWindow = new ReputationHistoryWindow(client.OriginalUser.Dni);
                historyWindow.ShowDialog();
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var generalHistoryWindow = new GeneralReputationHistoryWindow();
            generalHistoryWindow.ShowDialog();
        }

        private async Task<string?> ShowReputationDialog(string title, string message, string type, int points)
        {
            var dialog = new ReputationDialog(title, message, type, points);
            if (dialog.ShowDialog() == true)
            {
                return dialog.Motivo;
            }
            return null;
        }

        private async Task ApplyReputationChange(ReputationUserViewModel client, string tipo, string motivo, int puntos)
        {
            try
            {
                StatusText.Text = $"Aplicando {tipo}...";

                var requestData = new
                {
                    usuarioDni = client.Dni,
                    tipo = tipo,
                    motivo = motivo,
                    puntos = puntos
                };

                var response = await _apiService.CreateReputacionAsync(requestData);

                if (response.Success)
                {
                    // Intentar obtener la nueva reputación de la respuesta si está disponible
                    var newReputation = client.Reputacion + puntos; // Fallback si no hay respuesta

                    MessageBox.Show(
                        $"{(tipo == "sancion" ? "Sanción" : "Recompensa")} aplicada exitosamente\n" +
                        $"Cliente: {client.NombreCompleto}\n" +
                        $"Motivo: {motivo}\n" +
                        $"Puntos: {(puntos > 0 ? "+" : "")}{puntos}\n" +
                        $"Reputación anterior: {client.Reputacion}\n" +
                        $"Nueva reputación: {newReputation}",
                        $"{(tipo == "sancion" ? "Sanción" : "Recompensa")} Aplicada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Recargar datos
                    LoadClientsAsync();

                    // Notificar a UsuariosWindow para que refresque sus datos si está abierta
                    UsuariosWindow.CurrentInstance?.RefreshUserData();
                }
                else
                {
                    MessageBox.Show($"Error al aplicar {tipo}: {response.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                StatusText.Text = "Listo";
            }
        }


        private void ClientsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is ReputationUserViewModel selectedClient)
            {
                StatusText.Text = $"Seleccionado: {selectedClient.NombreCompleto} - Reputación: {selectedClient.Reputacion}";
            }
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

    // ViewModel para usuarios en el sistema de reputación
    public class ReputationUserViewModel : INotifyPropertyChanged
    {
        public Usuario OriginalUser { get; }

        public ReputationUserViewModel(Usuario user)
        {
            OriginalUser = user ?? throw new ArgumentNullException(nameof(user));
        }

        // Propiedades expuestas
        public string Dni => OriginalUser.Dni;
        public string NombreCompleto => OriginalUser.NombreCompleto;
        public string Email => OriginalUser.Email;
        public int Reputacion => OriginalUser.Reputacion;
        public string TipoMembresia => OriginalUser.TipoMembresia;
        public string FechaRegistroFormateada => OriginalUser.FechaRegistroFormateada;

        // Propiedades calculadas para el sistema de reputación
        public bool EsReputacionAlta => Reputacion >= 80;
        public bool EsReputacionMedia => Reputacion >= 50 && Reputacion < 80;
        public bool EsReputacionBaja => Reputacion < 50;
        public bool PuedeRecibirPremio => Reputacion >= 90;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}