using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    // Ventana principal de control de costos
    // Permite ver, filtrar y gestionar todos los costos de reparación y mantenimiento
    public partial class CostosWindow : Window
    {
        private readonly ApiService _apiService;
        private ObservableCollection<CostoViewModel> _costos = new();
        private List<CostoViewModel> _todosLosCostos = new();

        public CostosWindow()
        {
            InitializeComponent();

            _apiService = new ApiService();
            _apiService.UpdateAuthToken();

            // Configurar filtros de fecha por defecto (último mes)
            FechaHastaFilter.SelectedDate = DateTime.Now;
            FechaDesdeFilter.SelectedDate = DateTime.Now.AddMonths(-1);

            LoadCostosAsync();
        }

        // Cargo todos los costos desde la base de datos
        private async void LoadCostosAsync()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                EmptyPanel.Visibility = Visibility.Collapsed;
                StatusText.Text = "Cargando gastos...";

                var result = await _apiService.GetAllCostosAsync();

                if (result.Success && result.Data != null)
                {
                    // Usar JsonSerializerSettings con NullValueHandling para evitar errores con objetos anidados
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
                    };

                    var costos = JsonConvert.DeserializeObject<List<Costo>>(
                        JsonConvert.SerializeObject(result.Data, settings), settings) ?? new List<Costo>();

                    _todosLosCostos.Clear();
                    foreach (var costo in costos)
                    {
                        _todosLosCostos.Add(new CostoViewModel(costo));
                    }

                    AplicarFiltros();
                    CalcularEstadisticas();

                    LoadingPanel.Visibility = Visibility.Collapsed;

                    if (_costos.Count == 0)
                    {
                        EmptyPanel.Visibility = Visibility.Visible;
                    }

                    StatusText.Text = $"Última actualización: {DateTime.Now:HH:mm:ss} - {_costos.Count} registros encontrados";
                }
                else
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Error al cargar gastos: {result.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText.Text = "Error al cargar datos";
                }
            }
            catch (Exception ex)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show($"Error al cargar gastos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error al cargar datos";
            }
        }

        // Generar datos de ejemplo (en producción esto vendría de la API)
        private List<Costo> GenerarCostosDummy()
        {
            var random = new Random();
            var costos = new List<Costo>();
            var tipos = new[] { "Reparacion", "Mantenimiento", "Repuesto", "ManoDeObra" };
            var proveedores = new[] { "TechFit Solutions", "GymPro Supply", "FitEquip Corp", "Sport Repairs SA" };
            var tecnicos = new[] { "Juan Pérez", "María González", "Carlos Ruiz", "Ana Martínez" };
            var descripciones = new Dictionary<string, string[]>
            {
                { "Reparacion", new[] { "Reparación motor cinta #5", "Cambio de correa bicicleta #12", "Ajuste sistema hidráulico prensa", "Reparación pantalla elíptica #3" } },
                { "Mantenimiento", new[] { "Mantenimiento preventivo mensual", "Lubricación sistema rodamientos", "Calibración sensores", "Limpieza profunda equipos cardio" } },
                { "Repuesto", new[] { "Correa de transmisión nueva", "Rodamientos industriales set x4", "Cable de acero 5mm x 10m", "Pantalla LCD de repuesto" } },
                { "ManoDeObra", new[] { "Servicio técnico especializado", "Instalación de nuevo equipo", "Desmontaje y reacondicionamiento", "Diagnóstico técnico avanzado" } }
            };

            // Generar 50 registros de los últimos 6 meses
            for (int i = 0; i < 50; i++)
            {
                var tipo = tipos[random.Next(tipos.Length)];
                var fecha = DateTime.Now.AddDays(-random.Next(180));

                costos.Add(new Costo
                {
                    Id = Guid.NewGuid().ToString(),
                    TipoCosto = tipo,
                    Monto = random.Next(100, 5000) + (decimal)(random.NextDouble() * 0.99),
                    Fecha = fecha,
                    Descripcion = descripciones[tipo][random.Next(descripciones[tipo].Length)],
                    Proveedor = proveedores[random.Next(proveedores.Length)],
                    Tecnico = tecnicos[random.Next(tecnicos.Length)],
                    NumeroFactura = $"FC-{random.Next(1000, 9999)}",
                    Observaciones = i % 3 == 0 ? "Trabajo urgente completado" : null,
                    UsuarioRegistro = "Administrador",
                    CreatedAt = fecha,
                    UpdatedAt = fecha
                });
            }

            return costos.OrderByDescending(c => c.Fecha).ToList();
        }

        // Aplico los filtros seleccionados por el usuario
        private void AplicarFiltros()
        {
            var costosFiltered = _todosLosCostos.AsEnumerable();

            // Filtro por fecha
            if (FechaDesdeFilter.SelectedDate.HasValue)
            {
                costosFiltered = costosFiltered.Where(c => c.Fecha >= FechaDesdeFilter.SelectedDate.Value);
            }

            if (FechaHastaFilter.SelectedDate.HasValue)
            {
                var fechaHasta = FechaHastaFilter.SelectedDate.Value.AddDays(1).AddSeconds(-1);
                costosFiltered = costosFiltered.Where(c => c.Fecha <= fechaHasta);
            }

            // Filtro por tipo
            if (TipoFilter.SelectedItem is ComboBoxItem selectedTipo && selectedTipo.Tag != null)
            {
                var tipo = selectedTipo.Tag.ToString();
                costosFiltered = costosFiltered.Where(c => c.TipoCosto.Equals(tipo, StringComparison.OrdinalIgnoreCase));
            }

            _costos.Clear();
            foreach (var costo in costosFiltered.OrderByDescending(c => c.Fecha))
            {
                _costos.Add(costo);
            }

            CostosDataGrid.ItemsSource = _costos;
            EmptyPanel.Visibility = _costos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Calculo las estadísticas para mostrar en las tarjetas
        private void CalcularEstadisticas()
        {
            var costosEnRango = _costos.ToList();

            var totalGeneral = costosEnRango.Sum(c => c.Monto);
            var totalReparaciones = costosEnRango.Where(c => c.TipoCosto == "Reparacion").Sum(c => c.Monto);
            var totalMantenimiento = costosEnRango.Where(c => c.TipoCosto == "Mantenimiento").Sum(c => c.Monto);
            var totalRepuestos = costosEnRango.Where(c => c.TipoCosto == "Repuesto").Sum(c => c.Monto);
            var totalManoDeObra = costosEnRango.Where(c => c.TipoCosto == "ManoDeObra").Sum(c => c.Monto);

            var cantidadReparaciones = costosEnRango.Count(c => c.TipoCosto == "Reparacion");
            var cantidadMantenimiento = costosEnRango.Count(c => c.TipoCosto == "Mantenimiento");

            TotalGeneralText.Text = totalGeneral.ToString("C", new System.Globalization.CultureInfo("es-ES"));
            TotalReparacionesText.Text = totalReparaciones.ToString("C", new System.Globalization.CultureInfo("es-ES"));
            TotalMantenimientoText.Text = totalMantenimiento.ToString("C", new System.Globalization.CultureInfo("es-ES"));
            TotalRepuestosText.Text = totalRepuestos.ToString("C", new System.Globalization.CultureInfo("es-ES"));
            TotalManoDeObraText.Text = totalManoDeObra.ToString("C", new System.Globalization.CultureInfo("es-ES"));

            CantidadReparacionesText.Text = $"{cantidadReparaciones} registro{(cantidadReparaciones != 1 ? "s" : "")}";
            CantidadMantenimientoText.Text = $"{cantidadMantenimiento} registro{(cantidadMantenimiento != 1 ? "s" : "")}";
        }

        // Eventos de filtros
        private void FiltroFecha_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
                CalcularEstadisticas();
                StatusText.Text = $"Filtros aplicados - {_costos.Count} registros encontrados";
            }
        }

        private void TipoFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                AplicarFiltros();
                CalcularEstadisticas();
                StatusText.Text = $"Filtros aplicados - {_costos.Count} registros encontrados";
            }
        }

        // Botón para crear un nuevo costo
        private void NuevoCostoButton_Click(object sender, RoutedEventArgs e)
        {
            var nuevoCostoWindow = new NuevoCostoWindow();
            if (nuevoCostoWindow.ShowDialog() == true)
            {
                LoadCostosAsync();
            }
        }

        // Botón para actualizar los datos
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCostosAsync();
        }

        // Ver detalles de un costo
        private void VerDetalleCosto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CostoViewModel costo)
            {
                var detalles = $"Detalles del Costo\n\n" +
                             $"Tipo: {costo.TipoCostoCapitalizado}\n" +
                             $"Fecha: {costo.FechaFormateada}\n" +
                             $"Monto: {costo.MontoFormateado}\n\n" +
                             $"Descripción: {costo.Descripcion}\n\n" +
                             $"Proveedor: {costo.Proveedor ?? "N/A"}\n" +
                             $"Técnico: {costo.Tecnico ?? "N/A"}\n" +
                             $"N° Factura: {costo.NumeroFactura ?? "N/A"}\n\n" +
                             $"Observaciones: {costo.Observaciones ?? "Ninguna"}";

                MessageBox.Show(detalles, "Detalles del Costo",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Eliminar un costo
        private async void EliminarCosto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is CostoViewModel costo)
            {
                var result = MessageBox.Show(
                    $"¿Está seguro de eliminar este registro de costo?\n\n" +
                    $"Tipo: {costo.TipoCostoCapitalizado}\n" +
                    $"Descripción: {costo.Descripcion}\n" +
                    $"Monto: {costo.MontoFormateado}\n\n" +
                    $"Esta acción no se puede deshacer.",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusText.Text = "Eliminando costo...";

                        var deleteResult = await _apiService.DeleteCostoAsync(costo.Id);

                        if (deleteResult.Success)
                        {
                            _todosLosCostos.Remove(costo);
                            AplicarFiltros();
                            CalcularEstadisticas();

                            MessageBox.Show("Registro de costo eliminado exitosamente.",
                                          "Eliminación Exitosa",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Information);

                            StatusText.Text = $"Registro eliminado - {_costos.Count} registros restantes";
                        }
                        else
                        {
                            MessageBox.Show($"Error al eliminar: {deleteResult.ErrorMessage}", "Error",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                            StatusText.Text = "Error al eliminar";
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error de conexión: {ex.Message}", "Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusText.Text = "Error de conexión";
                    }
                }
            }
        }

        // Cerrar la ventana
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
