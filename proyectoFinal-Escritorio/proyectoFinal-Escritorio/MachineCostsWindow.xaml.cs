using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class MachineCostsWindow : Window
    {
        private readonly Maquina _machine;
        private readonly ApiService _apiService;
        private ObservableCollection<CostoViewModel> _costs = new();
        private List<CostoViewModel> _allCosts = new();

        public MachineCostsWindow(Maquina machine)
        {
            InitializeComponent();

            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            MachineHeaderText.Text = $"Gestión de Costos - {_machine.NumeroSerie}";

            InitializeFilters();
            LoadCostsAsync();
        }

        private void InitializeFilters()
        {
            var tiposCosto = new[] { "Todos los tipos", "Reparacion", "Mantenimiento", "Repuesto", "ManoDeObra", "Otro" };
            TipoCostoFilterComboBox.ItemsSource = tiposCosto;
            TipoCostoFilterComboBox.SelectedIndex = 0;
        }

        private async void LoadCostsAsync()
        {
            StatusText.Text = "Cargando gastos...";

            try
            {
                _allCosts.Clear();
                _costs.Clear();

                // Cargar TODOS los gastos desde la API usando ApiService
                var result = await _apiService.GetAllCostosAsync();

                if (result.Success && result.Data != null)
                {
                    // Usar JsonSerializerSettings con NullValueHandling para evitar errores con objetos anidados
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
                    };

                    var todosLosCostos = JsonConvert.DeserializeObject<List<Costo>>(
                        JsonConvert.SerializeObject(result.Data, settings), settings) ?? new List<Costo>();

                    // Filtrar solo los gastos de esta máquina
                    foreach (var costo in todosLosCostos)
                    {
                        // Comparar con el ID de la máquina
                        // El costo.MaquinaData puede ser un objeto con _id o un string con el ID
                        string? costoMaquinaId = null;

                        if (costo.MaquinaData is string str)
                        {
                            costoMaquinaId = str;
                        }
                        else if (costo.MaquinaData is JObject jo && jo["_id"] != null)
                        {
                            costoMaquinaId = jo["_id"]?.ToString();
                        }

                        // Comparar con el ID de la máquina actual
                        if (!string.IsNullOrEmpty(costoMaquinaId) &&
                            !string.IsNullOrEmpty(_machine.Id) &&
                            costoMaquinaId == _machine.Id)
                        {
                            _allCosts.Add(new CostoViewModel(costo));
                        }
                    }

                    FilterCosts();
                    UpdateStatistics();
                    StatusText.Text = $"Costos cargados: {_allCosts.Count} registros para {_machine.NumeroSerie}";
                }
                else
                {
                    MessageBox.Show($"Error al cargar gastos: {result.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Error al cargar gastos";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar gastos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error al cargar gastos";
            }
        }

        private void FilterCosts()
        {
            var filtered = _allCosts.AsEnumerable();

            // Filtro por tipo de costo
            if (TipoCostoFilterComboBox.SelectedItem is string tipoSeleccionado && tipoSeleccionado != "Todos los tipos")
            {
                filtered = filtered.Where(c => c.TipoCosto.Equals(tipoSeleccionado, StringComparison.OrdinalIgnoreCase));
            }

            _costs = new ObservableCollection<CostoViewModel>(filtered.ToList());
            CostsDataGrid.ItemsSource = _costs;
        }

        private void UpdateStatistics()
        {
            // Usar _allCosts para calcular las estadísticas totales de la máquina
            var costoReparacion = _allCosts.Where(c => c.TipoCosto.Equals("Reparacion", StringComparison.OrdinalIgnoreCase)).Sum(c => c.Monto);
            var costoMantenimiento = _allCosts.Where(c => c.TipoCosto.Equals("Mantenimiento", StringComparison.OrdinalIgnoreCase)).Sum(c => c.Monto);

            // Calcular el total sumando TODOS los costos de la máquina
            var costoTotal = _allCosts.Sum(c => c.Monto);

            TotalRepairCostsText.Text = costoReparacion.ToString("C");
            TotalMaintenanceCostsText.Text = costoMantenimiento.ToString("C");
            TotalCostsText.Text = costoTotal.ToString("C");
            TotalCountText.Text = _allCosts.Count.ToString();
        }

        private void UpdateStatus()
        {
            StatusText.Text = "Listo";
            LastUpdateText.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                FilterCosts();
                UpdateStatistics();
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            TipoCostoFilterComboBox.SelectedIndex = 0;
            FilterCosts();
            UpdateStatistics();
        }

        private void ViewCostButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CostoViewModel cost)
            {
                var details = $"Tipo: {cost.TipoCostoCapitalizado}\n" +
                             $"Monto: {cost.MontoFormateado}\n" +
                             $"Fecha: {cost.FechaFormateada}\n" +
                             $"Descripción: {cost.Descripcion}\n" +
                             $"Proveedor: {cost.Proveedor}\n" +
                             $"Factura: {cost.NumeroFactura}";

                MessageBox.Show(details, "Detalles del Costo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeleteCostButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CostoViewModel cost)
            {
                var result = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar este costo de {cost.MontoFormateado}?",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusText.Text = "Eliminando costo...";
                        // TODO: Llamar a API para eliminar costo
                        // await _apiService.DeleteCostAsync(cost.Id);

                        _costs.Remove(cost);
                        _allCosts.Remove(cost);
                        FilterCosts();
                        UpdateStatistics();

                        MessageBox.Show("Costo eliminado exitosamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar costo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        StatusText.Text = "Listo";
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }
}
