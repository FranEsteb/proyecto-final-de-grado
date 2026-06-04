using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    // Ventana simplificada de gestión de averías
    // He diseñado esta clase para ser simple y directa:
    // - Lista de averías pendientes con botón check
    // - Historial de averías resueltas
    // - Operaciones básicas sin complejidad
    public partial class AveriasSimpleWindow : Window, INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private ObservableCollection<AveriaSimpleViewModel> _averiasPendientes = new();
        private ObservableCollection<AveriaSimpleViewModel> _averiasResueltas = new();
        private ObservableCollection<AveriaSimpleViewModel> _reparacionesProgramadas = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public AveriasSimpleWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Verificar permisos de acceso
            if (!PermissionHelper.CanAccessAdminSection())
            {
                MessageBox.Show("❌ No tienes permisos para acceder a la gestión de averías. Solo administradores pueden acceder.",
                              "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Loaded += (s, e) => this.Close();
                return;
            }

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            LoadAveriasAsync();
        }

        // Cargo todas las averías y las separo en pendientes y resueltas
        private async void LoadAveriasAsync()
        {
            try
            {
                StatusText.Text = "Cargando averías...";

                var result = await _apiService.GetAllAveriasAsync();

                if (result.Success && result.Data != null)
                {
                    var averias = result.Data;

                    // Debug: Verificar que las averías tienen ID
                    foreach (var av in averias.Take(3))
                    {
                        System.Diagnostics.Debug.WriteLine($"Avería cargada - ID: '{av.Id}', Elemento: {av.ElementoAfectado}");
                    }

                    // Separo las averías en pendientes, resueltas y reparaciones programadas
                    var pendientes = averias.Where(a => a.EsPendiente && string.IsNullOrEmpty(a.TecnicoAsignado))
                                           .Select(a => new AveriaSimpleViewModel(a)).ToList();
                    var resueltas = averias.Where(a => a.EstaResuelta).Select(a => new AveriaSimpleViewModel(a))
                                          .OrderByDescending(a => a.FechaResolucion).ToList();
                    var reparaciones = averias.Where(a => a.EsPendiente && !string.IsNullOrEmpty(a.TecnicoAsignado))
                                             .Select(a => new AveriaSimpleViewModel(a)).ToList();

                    _averiasPendientes.Clear();
                    _averiasResueltas.Clear();
                    _reparacionesProgramadas.Clear();

                    foreach (var averia in pendientes)
                        _averiasPendientes.Add(averia);

                    foreach (var averia in resueltas)
                        _averiasResueltas.Add(averia);

                    foreach (var averia in reparaciones)
                        _reparacionesProgramadas.Add(averia);

                    UpdateDataGrids();
                    UpdateCounters();

                    StatusText.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    MessageBox.Show($"Error al cargar averías: {result.ErrorMessage}",
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

        // Actualizo los DataGrid con los datos
        private void UpdateDataGrids()
        {
            PendientesDataGrid.ItemsSource = _averiasPendientes;
            ResueltasDataGrid.ItemsSource = _averiasResueltas;
            ReparacionesDataGrid.ItemsSource = _reparacionesProgramadas;
        }

        // Actualizo los contadores en las pestañas
        private void UpdateCounters()
        {
            // Contadores de averías pendientes
            var totalPendientes = _averiasPendientes.Count;
            var altaPrioridad = _averiasPendientes.Count(a => a.Prioridad == "alta");
            var mediaPrioridad = _averiasPendientes.Count(a => a.Prioridad == "media");
            var bajaPrioridad = _averiasPendientes.Count(a => a.Prioridad == "baja");

            PendientesCountText.Text = $"{totalPendientes} averías pendientes";
            AltaPrioridadText.Text = $"{altaPrioridad} Alta";
            MediaPrioridadText.Text = $"{mediaPrioridad} Media";
            BajaPrioridadText.Text = $"{bajaPrioridad} Baja";

            // Contador de averías resueltas
            ResueltasCountText.Text = $"{_averiasResueltas.Count} averías resueltas";
            
            // Contador de reparaciones programadas
            ReparacionesCountText.Text = $"{_reparacionesProgramadas.Count} reparaciones programadas";
        }

        // Marco una avería como solucionada
        private async void MarcarSolucionadaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AveriaSimpleViewModel averia)
            {
                var result = MessageBox.Show(
                    $"¿Marcar como solucionada la avería?\n\n" +
                    $"Elemento: {averia.ElementoAfectado}\n" +
                    $"Problema: {averia.Descripcion}\n\n" +
                    $"Esta acción marcará la avería como resuelta.",
                    "Confirmar Resolución",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await MarcarComoResueltaAsync(averia);
                }
            }
        }

        // Envío la actualización a la API para marcar como resuelta
        private async Task MarcarComoResueltaAsync(AveriaSimpleViewModel averia)
        {
            try
            {
                StatusText.Text = "Marcando como solucionada...";

                // Creo el objeto actualizado
                var averiaActualizada = averia.OriginalAveria;

                // Debug: Verificar que tenemos el ID
                System.Diagnostics.Debug.WriteLine($"ID de la avería: '{averiaActualizada.Id}'");

                averiaActualizada.Estado = "resuelta";
                averiaActualizada.FechaResolucion = DateTime.Now;
                averiaActualizada.UpdatedAt = DateTime.Now;

                var result = await _apiService.UpdateAveriaAsync(averiaActualizada);

                if (result.Success)
                {
                    MessageBox.Show($"Avería marcada como solucionada:\n{averia.ElementoAfectado}",
                                  "Avería Resuelta", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Recargo los datos para actualizar las listas
                    LoadAveriasAsync();
                }
                else
                {
                    MessageBox.Show($"Error al actualizar: {result.ErrorMessage}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                StatusText.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";
            }
        }

        // Creo una nueva avería
        private void NewAveriaButton_Click(object sender, RoutedEventArgs e)
        {
            var nuevaAveriaWindow = new NuevaAveriaSimpleWindow();
            if (nuevaAveriaWindow.ShowDialog() == true)
            {
                LoadAveriasAsync(); // Recargar después de crear
            }
        }

        // Actualizo los datos
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadAveriasAsync();
        }

        // Vuelvo a la ventana anterior
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Gestión de reparaciones

        private void ProgramarReparacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_averiasPendientes.Count == 0)
            {
                MessageBox.Show("No hay averías pendientes para programar reparaciones.", 
                              "Sin Averías", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Mostrar lista de averías pendientes para seleccionar
            var averiasList = string.Join("\n", _averiasPendientes.Select((a, i) => $"{i + 1}. {a.ElementoAfectado} - {a.Descripcion}"));
            var result = MessageBox.Show($"Averías pendientes:\n\n{averiasList}\n\nTodas las averías pendientes se programarán para reparación. ¿Continuar?", 
                                       "Programar Reparaciones", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Mover todas las averías pendientes a reparaciones programadas
                var averiasAProgramar = _averiasPendientes.ToList();
                foreach (var averia in averiasAProgramar)
                {
                    averia.OriginalAveria.TecnicoAsignado = "";
                    averia.OriginalAveria.EstadoReparacion = "programada";
                    averia.OriginalAveria.FechaProgramada = DateTime.Now.AddDays(1);

                    _averiasPendientes.Remove(averia);
                    _reparacionesProgramadas.Add(averia);
                }
                
                UpdateDataGrids();
                UpdateCounters();
                
                MessageBox.Show($"{averiasAProgramar.Count} averías programadas para reparación.", 
                              "Reparaciones Programadas", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void TecnicoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is AveriaSimpleViewModel averia)
            {
                var selectedItem = comboBox.SelectedItem as ComboBoxItem;
                var tecnico = selectedItem?.Tag?.ToString() ?? "";
                
                averia.OriginalAveria.TecnicoAsignado = tecnico;
            }
        }

        private void FechaProgramada_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DatePicker datePicker && datePicker.Tag is AveriaSimpleViewModel averia)
            {
                averia.OriginalAveria.FechaProgramada = datePicker.SelectedDate;
            }
        }

        private void EstadoReparacion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is AveriaSimpleViewModel averia)
            {
                var selectedItem = comboBox.SelectedItem as ComboBoxItem;
                var estado = selectedItem?.Tag?.ToString() ?? "programada";
                
                averia.OriginalAveria.EstadoReparacion = estado;
                
                // Si se marca como completada, mover a resueltas
                if (estado == "completada")
                {
                    averia.OriginalAveria.Estado = "resuelta";
                    averia.OriginalAveria.FechaResolucion = DateTime.Now;
                }
            }
        }

        private async void GuardarReparacionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AveriaSimpleViewModel averia)
            {
                try
                {
                    StatusText.Text = "Guardando reparación...";

                    var result = await _apiService.UpdateAveriaAsync(averia.OriginalAveria);

                    if (result.Success)
                    {
                        MessageBox.Show($"Reparación actualizada:\n{averia.ElementoAfectado}",
                                      "Reparación Guardada", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Si se completó, recargar para mover a historial
                        if (averia.OriginalAveria.EstadoReparacion == "completada")
                        {
                            LoadAveriasAsync();
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Error al guardar: {result.ErrorMessage}", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error de conexión: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    StatusText.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }

    // ViewModel simplificado para averías
    // Esta clase adapta el modelo Averia para la interfaz simplificada
    public class AveriaSimpleViewModel : INotifyPropertyChanged
    {
        public Averia OriginalAveria { get; }

        public AveriaSimpleViewModel(Averia averia)
        {
            OriginalAveria = averia ?? throw new ArgumentNullException(nameof(averia));
        }

        // Propiedades principales
        public string ElementoAfectado => OriginalAveria.ElementoAfectado;
        public string Descripcion => OriginalAveria.Descripcion;
        public DateTime FechaReporte => OriginalAveria.FechaReporte;
        public string Prioridad => OriginalAveria.Prioridad;
        public string Estado => OriginalAveria.Estado;
        public DateTime? FechaResolucion => OriginalAveria.FechaResolucion;
        public string? Observaciones => OriginalAveria.Observaciones;
        
        // Propiedades de reparación
        public string? TecnicoAsignado 
        { 
            get => OriginalAveria.TecnicoAsignado; 
            set 
            { 
                OriginalAveria.TecnicoAsignado = value; 
                OnPropertyChanged(nameof(TecnicoAsignado)); 
            } 
        }
        public DateTime? FechaProgramada 
        { 
            get => OriginalAveria.FechaProgramada; 
            set 
            { 
                OriginalAveria.FechaProgramada = value; 
                OnPropertyChanged(nameof(FechaProgramada)); 
            } 
        }
        public string EstadoReparacion 
        { 
            get => OriginalAveria.EstadoReparacion; 
            set 
            { 
                OriginalAveria.EstadoReparacion = value; 
                OnPropertyChanged(nameof(EstadoReparacion)); 
            } 
        }
        public decimal? CostoReparacion => OriginalAveria.CostoReparacion;

        // Propiedades calculadas para la interfaz
        public string PrioridadCapitalizada => char.ToUpper(Prioridad[0]) + Prioridad.Substring(1);
        public string CostoReparacionFormateado => CostoReparacion.HasValue ? CostoReparacion.Value.ToString("C", new System.Globalization.CultureInfo("es-ES")) : "No especificado";
        public string EstadoCapitalizado => char.ToUpper(Estado[0]) + Estado.Substring(1);
        public string FechaReporteFormateada => FechaReporte.ToString("dd/MM/yyyy");
        public string FechaResolucionFormateada => FechaResolucion?.ToString("dd/MM/yyyy") ?? "";
        public string FechaProgramadaFormateada => FechaProgramada?.ToString("dd/MM/yyyy") ?? "Sin programar";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}