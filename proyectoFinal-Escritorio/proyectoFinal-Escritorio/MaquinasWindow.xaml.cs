using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    // Esta ventana sirve para gestionar todas las máquinas del gimnasio.
    // Aquí puedo ver, crear, editar y eliminar máquinas, además de filtrar y buscar entre ellas.
    public partial class MaquinasWindow : Window, INotifyPropertyChanged
    {
        // Servicio que maneja todas las llamadas a la API
        private readonly ApiService _apiService;
        
        // Aquí guardo todas las máquinas que cargo de la API.
        // Uso dos listas: una con todas las máquinas y otra con las que se muestran después de filtrar.
        private ObservableCollection<MaquinaViewModel> _machines = new();
        private ObservableCollection<MaquinaViewModel> _filteredMachines = new();
        
        // Listas con los valores únicos para los filtros de tipo, estado y ubicación
        private List<string> _tipos = new();
        private List<string> _estados = new();
        private List<string> _ubicaciones = new();
        
        // El texto que el usuario escribe en la caja de búsqueda
        private string _searchText = "";

        // Esta propiedad conecta la lista filtrada con la tabla que ve el usuario
        public ObservableCollection<MaquinaViewModel> FilteredMachines
        {
            get => _filteredMachines;
            set
            {
                _filteredMachines = value;
                OnPropertyChanged(nameof(FilteredMachines));
            }
        }

        // Cuando el usuario escribe algo en la búsqueda, automáticamente filtro la lista.
        // Esto hace que la búsqueda sea instantánea mientras el usuario escribe.
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterMachines();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public MaquinasWindow()
        {
            InitializeComponent();
            DataContext = this;

            _apiService = new ApiService();

            InitializeFilters();
            LoadMachinesAsync();

            // Set placeholder text for search
            SetPlaceholderText();
        }

        private void InitializeFilters()
        {
            // Inicializar filtros de tipo
            var tipos = new List<string> { "Todos los tipos" };
            tipos.AddRange(Maquina.TiposDisponibles.Select(t => char.ToUpper(t[0]) + t.Substring(1)));
            TipoFilterComboBox.ItemsSource = tipos;
            TipoFilterComboBox.SelectedIndex = 0;

            // Inicializar filtros de estado
            var estados = new List<string> { "Todos los estados" };
            estados.AddRange(Maquina.EstadosDisponibles.Select(e => char.ToUpper(e[0]) + e.Substring(1)));
            EstadoFilterComboBox.ItemsSource = estados;
            EstadoFilterComboBox.SelectedIndex = 0;

            // Inicializar filtros de ubicación
            var ubicaciones = new List<string> { "Todas las ubicaciones" };
            UbicacionFilterComboBox.ItemsSource = ubicaciones;
            UbicacionFilterComboBox.SelectedIndex = 0;
        }

        private void SetPlaceholderText()
        {
            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                SearchTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
                SearchTextBox.Text = "Buscar por número de serie, marca, modelo...";
                SearchTextBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
            }
        }

        private async void LoadMachinesAsync()
        {
            StatusText.Text = "Cargando máquinas...";
            
            var result = await _apiService.GetAllMachinesAsync();
            
            if (result.Success)
            {
                _machines.Clear();
                foreach (var machine in result.Data ?? new List<Maquina>())
                {
                    _machines.Add(new MaquinaViewModel(machine));
                }
                
                UpdateFilterOptions();
                FilterMachines();
                UpdateStatus();
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error al cargar datos";
            }
        }

        private void UpdateFilterOptions()
        {
            // Actualizar tipos disponibles
            var tiposActuales = _machines.Select(m => m.TipoCapitalizado).Distinct().OrderBy(t => t).ToList();
            var tiposList = new List<string> { "Todos los tipos" };
            tiposList.AddRange(tiposActuales);
            TipoFilterComboBox.ItemsSource = tiposList;

            // Actualizar ubicaciones disponibles
            var ubicacionesActuales = _machines.Where(m => !string.IsNullOrEmpty(m.Ubicacion))
                                              .Select(m => m.Ubicacion).Distinct().OrderBy(u => u).ToList();
            var ubicacionesList = new List<string> { "Todas las ubicaciones" };
            ubicacionesList.AddRange(ubicacionesActuales);
            UbicacionFilterComboBox.ItemsSource = ubicacionesList;
        }

        private void FilterMachines()
        {
            var filtered = _machines.AsEnumerable();

            // Filtro por texto de búsqueda
            if (!string.IsNullOrEmpty(SearchText) && SearchText != "Buscar por número de serie, marca, modelo...")
            {
                filtered = filtered.Where(m => 
                    m.NumeroSerie.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (m.Marca?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (m.Modelo?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    m.TipoCapitalizado.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por tipo
            if (TipoFilterComboBox.SelectedItem is string tipoSeleccionado && tipoSeleccionado != "Todos los tipos")
            {
                filtered = filtered.Where(m => m.TipoCapitalizado.Equals(tipoSeleccionado, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por estado
            if (EstadoFilterComboBox.SelectedItem is string estadoSeleccionado && estadoSeleccionado != "Todos los estados")
            {
                filtered = filtered.Where(m => m.EstadoCapitalizado.Equals(estadoSeleccionado, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por ubicación
            if (UbicacionFilterComboBox.SelectedItem is string ubicacionSeleccionada && ubicacionSeleccionada != "Todas las ubicaciones")
            {
                filtered = filtered.Where(m => m.Ubicacion != null && m.Ubicacion.Equals(ubicacionSeleccionada, StringComparison.OrdinalIgnoreCase));
            }

            FilteredMachines = new ObservableCollection<MaquinaViewModel>(filtered.ToList());
            MachinesDataGrid.ItemsSource = FilteredMachines;
            
            UpdateMachineCount();
        }

        private void UpdateMachineCount()
        {
            var total = _machines.Count;
            var filtered = FilteredMachines.Count;
            var operativas = FilteredMachines.Count(m => m.Estado.Equals("operativa", StringComparison.OrdinalIgnoreCase));

            if (filtered == total)
            {
                MachineCountText.Text = $"{total} máquinas registradas ({operativas} operativas)";
            }
            else
            {
                MachineCountText.Text = $"{filtered} de {total} máquinas mostradas ({operativas} operativas)";
            }
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";
            LastUpdateText.Text = $"{_machines.Count} máquinas cargadas";
        }

        // Event Handlers
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMachinesAsync();
        }

        private void NewMachineButton_Click(object sender, RoutedEventArgs e)
        {
            var machineForm = new MachineFormWindow(MachineFormMode.Create, null);
            if (machineForm.ShowDialog() == true)
            {
                LoadMachinesAsync(); // Recargar lista después de crear
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.Text == "Buscar por número de serie, marca, modelo...")
            {
                return; // No procesar el texto placeholder
            }
            FilterMachines();
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.Text == "Buscar por número de serie, marca, modelo...")
            {
                textBox.Text = "";
                textBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrEmpty(textBox?.Text))
            {
                SetPlaceholderText();
            }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterMachines();
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            TipoFilterComboBox.SelectedIndex = 0;
            EstadoFilterComboBox.SelectedIndex = 0;
            UbicacionFilterComboBox.SelectedIndex = 0;
            SetPlaceholderText();
            FilterMachines();
        }

        private void ViewMachineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MaquinaViewModel machine)
            {
                var machineForm = new MachineFormWindow(MachineFormMode.View, machine.OriginalMachine);
                machineForm.ShowDialog();
            }
        }

        private void EditMachineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MaquinaViewModel machine)
            {
                var machineForm = new MachineFormWindow(MachineFormMode.Edit, machine.OriginalMachine);
                if (machineForm.ShowDialog() == true)
                {
                    LoadMachinesAsync(); // Recargar lista después de editar
                }
            }
        }

        private void UpdateStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MaquinaViewModel machine)
            {
                var stateWindow = new MachineStateWindow(machine.OriginalMachine);
                if (stateWindow.ShowDialog() == true)
                {
                    LoadMachinesAsync(); // Recargar lista después de actualizar estado
                }
            }
        }

        private void ManageCostsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MaquinaViewModel machine)
            {
                var costsWindow = new MachineCostsWindow(machine.OriginalMachine);
                costsWindow.ShowDialog();
            }
        }

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MaquinaViewModel machine)
            {
                var historyWindow = new MachineHistoryWindow(machine.OriginalMachine);
                historyWindow.ShowDialog();
            }
        }

        private async void DeleteMachineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MaquinaViewModel machine)
            {
                var result = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar la máquina '{machine.NumeroSerie}'?\n\nEsta acción no se puede deshacer.",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await DeleteMachineAsync(machine.OriginalMachine);
                }
            }
        }

        private async Task DeleteMachineAsync(Maquina machine)
        {
            StatusText.Text = "Eliminando máquina...";

            var result = await _apiService.DeleteMachineAsync(machine.NumeroSerie);

            if (result.Success)
            {
                MessageBox.Show("Máquina eliminada exitosamente", 
                              "Eliminación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadMachinesAsync(); // Recargar lista
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            StatusText.Text = "Listo";
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new MachineStatsWindow();
            statsWindow.ShowDialog();
        }

        private void MachinesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MachinesDataGrid.SelectedItem is MaquinaViewModel selectedMachine)
            {
                StatusText.Text = $"Seleccionada: {selectedMachine.NumeroSerie} - {selectedMachine.TipoCapitalizado}";
            }
        }

        private void MachinesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MachinesDataGrid.SelectedItem is MaquinaViewModel machine)
            {
                ViewMachineButton_Click(new Button { Tag = machine }, new RoutedEventArgs());
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiService.Dispose();
            base.OnClosed(e);
        }
    }
}