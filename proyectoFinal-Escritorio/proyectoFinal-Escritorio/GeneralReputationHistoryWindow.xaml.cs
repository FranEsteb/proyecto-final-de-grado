using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class GeneralReputationHistoryWindow : Window, INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private ObservableCollection<GeneralReputationHistoryViewModel> _allRecords = new();
        private ObservableCollection<GeneralReputationHistoryViewModel> _filteredRecords = new();
        private string _searchText = "";

        public ObservableCollection<GeneralReputationHistoryViewModel> FilteredRecords
        {
            get => _filteredRecords;
            set
            {
                _filteredRecords = value;
                OnPropertyChanged(nameof(FilteredRecords));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterRecords();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public GeneralReputationHistoryWindow()
        {
            InitializeComponent();
            DataContext = this;

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            LoadAllRecordsAsync();
        }


        private async void LoadAllRecordsAsync()
        {
            try
            {
                StatusText.Text = "Cargando registros...";

                // Cargar todos los registros de reputación usando ApiService
                var reputacionResult = await _apiService.GetAllReputacionAsync();

                if (reputacionResult.Success && reputacionResult.Data != null)
                {
                    try
                    {
                        var reputacionRecords = JsonConvert.DeserializeObject<List<ReputacionRecord>>(JsonConvert.SerializeObject(reputacionResult.Data)) ?? new List<ReputacionRecord>();

                        // Cargar usuarios para obtener nombres usando ApiService
                        var usuariosResult = await _apiService.GetAllUsersAsync();
                        var usuariosDict = new Dictionary<string, Usuario>();

                        if (usuariosResult.Success && usuariosResult.Data != null)
                        {
                            var usuarios = JsonConvert.DeserializeObject<List<Usuario>>(JsonConvert.SerializeObject(usuariosResult.Data)) ?? new List<Usuario>();
                            usuariosDict = usuarios.ToDictionary(u => u.Dni, u => u);
                        }

                        _allRecords.Clear();

                        // Ordenar usando Comparison<T> explícito para evitar problemas con dynamic
                        Comparison<ReputacionRecord> comparison = (a, b) => b.Fecha.CompareTo(a.Fecha);
                        reputacionRecords.Sort(comparison);

                        foreach (var record in reputacionRecords)
                        {
                            var viewModel = new GeneralReputationHistoryViewModel(record);

                            // Si el usuario ya viene poblado en el record, extraer la info directamente
                            if (record.UsuarioData is JObject jo)
                            {
                                var nombre = jo["nombre"]?.ToString() ?? "";
                                var apellidos = jo["apellidos"]?.ToString() ?? "";
                                var dni = jo["dni"]?.ToString() ?? "";

                                var nombreCompleto = !string.IsNullOrEmpty(nombre) && !string.IsNullOrEmpty(apellidos)
                                    ? $"{nombre} {apellidos}"
                                    : (nombre ?? "Usuario desconocido");

                                viewModel.SetUsuarioInfoDirect(nombreCompleto, dni);
                            }
                            // Si no viene poblado, buscar en el diccionario
                            else if (!string.IsNullOrEmpty(record.Usuario))
                            {
                                if (usuariosDict.TryGetValue(record.Usuario, out Usuario? usuario) && usuario != null)
                                {
                                    viewModel.SetUsuarioInfo(usuario);
                                }
                            }

                            _allRecords.Add(viewModel);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al procesar registros: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    FilterRecords();
                    UpdateStatistics();
                    UpdateStatus();
                }
                else
                {
                    MessageBox.Show($"Error al cargar registros: {reputacionResult.ErrorMessage}",
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

        private void FilterRecords()
        {
            var filtered = _allRecords.AsEnumerable();

            // Filtro por tipo (con null-check)
            if (TipoFilterComboBox != null)
            {
                if (TipoFilterComboBox.SelectedIndex == 1) // Solo recompensas
                {
                    filtered = filtered.Where(r => r.Tipo == "recompensa");
                }
                else if (TipoFilterComboBox.SelectedIndex == 2) // Solo sanciones
                {
                    filtered = filtered.Where(r => r.Tipo == "sancion");
                }
            }

            // Filtro por fecha (con null-check)
            if (FechaFilterComboBox != null)
            {
                var now = DateTime.Now;
                switch (FechaFilterComboBox.SelectedIndex)
                {
                    case 1: // Últimos 7 días
                        filtered = filtered.Where(r => r.Fecha >= now.AddDays(-7));
                        break;
                    case 2: // Último mes
                        filtered = filtered.Where(r => r.Fecha >= now.AddMonths(-1));
                        break;
                    case 3: // Últimos 3 meses
                        filtered = filtered.Where(r => r.Fecha >= now.AddMonths(-3));
                        break;
                }
            }

            FilteredRecords = new ObservableCollection<GeneralReputationHistoryViewModel>(filtered.OrderByDescending(r => r.Fecha));

            if (GeneralHistoryDataGrid != null)
            {
                GeneralHistoryDataGrid.ItemsSource = FilteredRecords;
            }

            UpdateRecordCount();
        }

        private void UpdateRecordCount()
        {
            if (RecordCountText == null) return;

            var total = _allRecords.Count;
            var filtered = FilteredRecords.Count;

            if (filtered == total)
            {
                RecordCountText.Text = $"{total} registros de reputación";
            }
            else
            {
                RecordCountText.Text = $"{filtered} de {total} registros mostrados";
            }
        }

        private void UpdateStatistics()
        {
            var recompensas = _allRecords.Count(r => r.Tipo == "recompensa");
            var sanciones = _allRecords.Count(r => r.Tipo == "sancion");
            var totalRegistros = _allRecords.Count;
            var usuariosUnicos = _allRecords.Select(r => r.UsuarioDni).Distinct().Count();

            // Calcular promedio de reputación (esto sería ideal hacerlo desde el servidor)
            var promedioReputacion = 75; // Valor placeholder

            if (TotalRecompensasText != null)
                TotalRecompensasText.Text = recompensas.ToString();

            if (TotalSancionesText != null)
                TotalSancionesText.Text = sanciones.ToString();

            if (TotalRegistrosText != null)
                TotalRegistrosText.Text = totalRegistros.ToString();

            if (UsuariosAfectadosText != null)
                UsuariosAfectadosText.Text = usuariosUnicos.ToString();

            if (PromedioReputacionText != null)
                PromedioReputacionText.Text = promedioReputacion.ToString();
        }

        private void UpdateStatus()
        {
            if (StatusText != null)
                StatusText.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";

            if (LastUpdateText != null)
                LastUpdateText.Text = $"{_allRecords.Count} registros cargados";
        }

        // Event Handlers
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAllRecordsAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void TipoFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TipoFilterComboBox != null)
            {
                FilterRecords();
            }
        }

        private void FechaFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FechaFilterComboBox != null)
            {
                FilterRecords();
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            if (TipoFilterComboBox != null)
            {
                TipoFilterComboBox.SelectedIndex = 0;
            }

            if (FechaFilterComboBox != null)
            {
                FechaFilterComboBox.SelectedIndex = 0;
            }

            FilterRecords();
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

    // ViewModel para registros del historial general
    public class GeneralReputationHistoryViewModel
    {
        public ReputacionRecord OriginalRecord { get; }
        
        public GeneralReputationHistoryViewModel(ReputacionRecord record)
        {
            OriginalRecord = record ?? throw new ArgumentNullException(nameof(record));
        }

        // Propiedades del registro
        public string IdRep => OriginalRecord.IdRep;
        public DateTime Fecha => OriginalRecord.Fecha;
        public string FechaFormateada => OriginalRecord.Fecha.ToString("dd/MM/yyyy HH:mm");
        public string Tipo => OriginalRecord.Tipo;
        public string Motivo => OriginalRecord.Motivo;
        public int Puntos => OriginalRecord.Puntos;
        public bool EsPositivo => Puntos > 0;
        public string PuntosFormateados => Puntos > 0 ? $"+{Puntos}" : Puntos.ToString();
        
        // Información del usuario (se establece por separado)
        public string UsuarioNombre { get; private set; } = "Usuario desconocido";
        public string UsuarioDni { get; private set; } = "N/A";

        public void SetUsuarioInfo(Usuario usuario)
        {
            if (usuario != null)
            {
                UsuarioNombre = usuario.NombreCompleto;
                UsuarioDni = usuario.Dni;
            }
        }

        public void SetUsuarioInfoDirect(string nombre, string dni)
        {
            UsuarioNombre = nombre;
            UsuarioDni = dni;
        }
    }

    // Modelo para deserializar registros de reputación del API
    public class ReputacionRecord
    {
        [JsonProperty("idRep")]
        public string IdRep { get; set; } = "";

        [JsonProperty("usuario")]
        public dynamic? UsuarioData { get; set; }

        // Propiedad para acceder al DNI del usuario
        [JsonIgnore]
        public string Usuario
        {
            get
            {
                if (UsuarioData == null) return "";
                if (UsuarioData is string str) return str;
                if (UsuarioData is JObject jo)
                {
                    // Si es un objeto, intentar extraer el DNI
                    var dni = jo["dni"]?.ToString() ?? jo["Dni"]?.ToString();
                    if (!string.IsNullOrEmpty(dni)) return dni;

                    // Si no hay DNI, intentar con el _id
                    return jo["_id"]?.ToString() ?? "";
                }
                return UsuarioData?.ToString() ?? "";
            }
        }

        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; }

        [JsonProperty("tipo")]
        public string Tipo { get; set; } = "";

        [JsonProperty("motivo")]
        public string Motivo { get; set; } = "";

        [JsonProperty("puntos")]
        public int Puntos { get; set; }
    }
}