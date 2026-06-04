using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace proyectoFinal_Escritorio.Models
{
    // Modelo para representar un técnico de mantenimiento y reparación
    public class Tecnico : INotifyPropertyChanged
    {
        [JsonProperty("_id")]
        public string? Id { get; set; }

        [JsonProperty("nombre")]
        public string Nombre { get; set; } = "";

        [JsonProperty("apellidos")]
        public string Apellidos { get; set; } = "";

        [JsonProperty("email")]
        public string Email { get; set; } = "";

        [JsonProperty("telefono")]
        public string Telefono { get; set; } = "";

        // Información profesional
        [JsonProperty("especialidad")]
        public string Especialidad { get; set; } = "General";

        [JsonProperty("certificaciones")]
        public string[]? Certificaciones { get; set; }

        // Estado
        [JsonProperty("activo")]
        public bool Activo { get; set; } = true;

        [JsonProperty("disponible")]
        public bool Disponible { get; set; } = true;

        // Información adicional
        [JsonProperty("fechaContratacion")]
        public DateTime? FechaContratacion { get; set; }

        [JsonProperty("tarifaHora")]
        public decimal? TarifaHora { get; set; }

        // Estadísticas
        [JsonProperty("reparacionesCompletadas")]
        public int ReparacionesCompletadas { get; set; }

        [JsonProperty("calificacionPromedio")]
        public double CalificacionPromedio { get; set; }

        [JsonProperty("notas")]
        public string? Notas { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        // Propiedades calculadas para la interfaz
        public string NombreCompleto => $"{Nombre} {Apellidos}";
        public string EstadoDisplay => Activo ? (Disponible ? "Disponible" : "No Disponible") : "Inactivo";
        public string TarifaFormateada => TarifaHora?.ToString("C") ?? "No especificada";
        public string CalificacionFormateada => $"{CalificacionPromedio:F1} ⭐";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
