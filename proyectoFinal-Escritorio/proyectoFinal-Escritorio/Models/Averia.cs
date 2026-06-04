using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace proyectoFinal_Escritorio.Models
{
    // Modelo simplificado de avería - solo campos esenciales
    // He simplificado este modelo para que sea fácil de usar y gestionar
    public class Averia : INotifyPropertyChanged
    {
        [JsonProperty("_id")]
        public string Id { get; set; } = "";
        public string ElementoAfectado { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public DateTime FechaReporte { get; set; }
        public string Prioridad { get; set; } = "media";
        public string Estado { get; set; } = "pendiente";
        public DateTime? FechaResolucion { get; set; }
        public string? Observaciones { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Campos para gestión de reparaciones
        public string? TecnicoAsignado { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public string EstadoReparacion { get; set; } = "programada";
        public decimal? CostoReparacion { get; set; }

        // Estados simplificados
        public static readonly string[] EstadosDisponibles = { "pendiente", "resuelta" };
        
        // Prioridades disponibles
        public static readonly string[] PrioridadesDisponibles = { "baja", "media", "alta" };

        // Propiedades calculadas para la interfaz
        public string EstadoCapitalizado => char.ToUpper(Estado[0]) + Estado.Substring(1);
        public string PrioridadCapitalizada => char.ToUpper(Prioridad[0]) + Prioridad.Substring(1);
        public string FechaReporteFormateada => FechaReporte.ToString("dd/MM/yyyy");
        public string FechaResolucionFormateada => FechaResolucion?.ToString("dd/MM/yyyy") ?? "";
        public bool EstaResuelta => Estado.Equals("resuelta", StringComparison.OrdinalIgnoreCase);
        public bool EsPendiente => Estado.Equals("pendiente", StringComparison.OrdinalIgnoreCase);

        // Mantengo compatibilidad con código existente
        public string Maquina 
        { 
            get => ElementoAfectado; 
            set => ElementoAfectado = value; 
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}