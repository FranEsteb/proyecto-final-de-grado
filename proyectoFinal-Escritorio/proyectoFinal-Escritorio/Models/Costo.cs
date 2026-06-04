using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace proyectoFinal_Escritorio.Models
{
    // Tipos de gasto disponibles en el sistema
    public enum TipoCosto
    {
        Reparacion,
        Mantenimiento,
        Repuesto,
        ManoDeObra,
        Otro
    }

    // Modelo para representar un registro de gasto
    public class Costo : INotifyPropertyChanged
    {
        [JsonProperty("_id")]
        public string? Id { get; set; }

        // Relación con máquina o avería (como IDs)
        [JsonProperty("maquina")]
        public dynamic? MaquinaData { get; set; }

        // Propiedad para acceder al ID o nombre de la máquina
        [JsonIgnore]
        public string? MaquinaId
        {
            get
            {
                if (MaquinaData == null) return null;
                if (MaquinaData is string str) return str;
                if (MaquinaData is JObject jo && jo["_id"] != null) return jo["_id"]?.ToString();
                return MaquinaData?.ToString();
            }
            set
            {
                // Permitir asignar el ID directamente
                MaquinaData = value;
            }
        }

        [JsonProperty("averia")]
        public dynamic? AveriaData { get; set; }

        // Propiedad para acceder al ID de la avería
        [JsonIgnore]
        public string? AveriaId
        {
            get
            {
                if (AveriaData == null) return null;
                if (AveriaData is string str) return str;
                if (AveriaData is JObject jo && jo["_id"] != null) return jo["_id"]?.ToString();
                return AveriaData?.ToString();
            }
            set
            {
                // Permitir asignar el ID directamente
                AveriaData = value;
            }
        }

        // Información del costo
        [JsonProperty("tipoCosto")]
        public string TipoCosto { get; set; } = "Reparacion";

        [JsonProperty("monto")]
        public decimal Monto { get; set; }

        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; }

        [JsonProperty("descripcion")]
        public string Descripcion { get; set; } = "";

        // IDs de relaciones - Pueden venir como string o como objeto desde la API
        [JsonProperty("proveedor")]
        public dynamic? ProveedorData { get; set; }

        // Propiedad para acceder al nombre del proveedor
        [JsonIgnore]
        public string? ProveedorId
        {
            get
            {
                if (ProveedorData == null) return null;
                if (ProveedorData is string str) return str;
                if (ProveedorData is JObject jo && jo["_id"] != null) return jo["_id"]?.ToString();
                return ProveedorData?.ToString();
            }
            set
            {
                // Permitir asignar el ID directamente
                ProveedorData = value;
            }
        }

        [JsonProperty("tecnico")]
        public dynamic? TecnicoData { get; set; }

        // Propiedad para acceder al ID del técnico
        [JsonIgnore]
        public string? TecnicoId
        {
            get
            {
                if (TecnicoData == null) return null;
                if (TecnicoData is string str) return str;
                if (TecnicoData is JObject jo && jo["_id"] != null) return jo["_id"]?.ToString();
                return TecnicoData?.ToString();
            }
            set
            {
                // Permitir asignar el ID directamente
                TecnicoData = value;
            }
        }

        // Información adicional
        [JsonProperty("numeroFactura")]
        public string? NumeroFactura { get; set; }

        [JsonProperty("observaciones")]
        public string? Observaciones { get; set; }

        // Auditoría
        [JsonProperty("usuarioRegistro")]
        public string? UsuarioRegistro { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        // Propiedades auxiliares para mostrar nombres (cuando vienen populados del servidor)
        [JsonIgnore]
        public string? Proveedor
        {
            get
            {
                if (ProveedorData == null) return null;

                if (ProveedorData is string str) return str;

                if (ProveedorData is JObject jo)
                {
                    // Si el objeto tiene un campo Nombre, devolverlo
                    if (jo["Nombre"] != null) return jo["Nombre"]?.ToString();
                    if (jo["nombre"] != null) return jo["nombre"]?.ToString();
                }

                return null;
            }
            set
            {
                // Permitir asignar el nombre del proveedor directamente
                // En caso de que sea necesario para compatibility
            }
        }

        [JsonIgnore]
        public string? Tecnico
        {
            get
            {
                if (TecnicoData == null) return null;

                if (TecnicoData is string str) return str;

                if (TecnicoData is JObject jo)
                {
                    // Si el objeto tiene campos de nombre, construir el nombre completo
                    var nombre = jo["nombre"]?.ToString() ?? jo["Nombre"]?.ToString();
                    var apellidos = jo["apellidos"]?.ToString() ?? jo["Apellidos"]?.ToString();

                    if (!string.IsNullOrEmpty(nombre) && !string.IsNullOrEmpty(apellidos))
                        return $"{nombre} {apellidos}";
                    if (!string.IsNullOrEmpty(nombre))
                        return nombre;
                }

                return null;
            }
            set
            {
                // Permitir asignar el nombre del técnico directamente
            }
        }

        // Propiedades calculadas para la interfaz
        public string MontoFormateado => Monto.ToString("C", new System.Globalization.CultureInfo("es-ES"));
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        public string TipoCostoCapitalizado => CapitalizeFirstLetter(TipoCosto);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }
    }

    // ViewModel para costos en la interfaz
    public class CostoViewModel : INotifyPropertyChanged
    {
        public Costo OriginalCosto { get; }

        public CostoViewModel(Costo costo)
        {
            OriginalCosto = costo ?? throw new ArgumentNullException(nameof(costo));
        }

        public string? Id => OriginalCosto.Id;
        public string TipoCosto => OriginalCosto.TipoCosto;
        public string TipoCostoCapitalizado => OriginalCosto.TipoCostoCapitalizado;
        public decimal Monto => OriginalCosto.Monto;
        public string MontoFormateado => OriginalCosto.MontoFormateado;
        public DateTime Fecha => OriginalCosto.Fecha;
        public string FechaFormateada => OriginalCosto.FechaFormateada;
        public string Descripcion => OriginalCosto.Descripcion;
        public string? Proveedor => OriginalCosto.Proveedor;
        public string? Tecnico => OriginalCosto.Tecnico;
        public string? NumeroFactura => OriginalCosto.NumeroFactura;
        public string? Observaciones => OriginalCosto.Observaciones;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Modelo para estadísticas de costos
    public class EstadisticasCostos
    {
        public decimal TotalReparaciones { get; set; }
        public decimal TotalMantenimiento { get; set; }
        public decimal TotalRepuestos { get; set; }
        public decimal TotalManoDeObra { get; set; }
        public decimal TotalOtros { get; set; }
        public decimal TotalGeneral { get; set; }

        public int CantidadReparaciones { get; set; }
        public int CantidadMantenimientos { get; set; }

        public decimal PromedioReparacion { get; set; }
        public decimal PromedioMantenimiento { get; set; }

        public string MaquinaMasCostosa { get; set; } = "";
        public decimal CostoMaquinaMasCostosa { get; set; }

        // Formatos para la interfaz
        public string TotalReparacionesFormateado => TotalReparaciones.ToString("C", new System.Globalization.CultureInfo("es-ES"));
        public string TotalMantenimientoFormateado => TotalMantenimiento.ToString("C", new System.Globalization.CultureInfo("es-ES"));
        public string TotalRepuestosFormateado => TotalRepuestos.ToString("C", new System.Globalization.CultureInfo("es-ES"));
        public string TotalManoDeObraFormateado => TotalManoDeObra.ToString("C", new System.Globalization.CultureInfo("es-ES"));
        public string TotalOtrosFormateado => TotalOtros.ToString("C", new System.Globalization.CultureInfo("es-ES"));
        public string TotalGeneralFormateado => TotalGeneral.ToString("C", new System.Globalization.CultureInfo("es-ES"));
        public string PromedioReparacionFormateado => PromedioReparacion.ToString("C", new System.Globalization.CultureInfo("es-ES"));
        public string PromedioMantenimientoFormateado => PromedioMantenimiento.ToString("C", new System.Globalization.CultureInfo("es-ES"));
        public string CostoMaquinaMasCostosaFormateado => CostoMaquinaMasCostosa.ToString("C", new System.Globalization.CultureInfo("es-ES"));
    }

    // Filtros para consulta de costos
    public class FiltrosCostos
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? TipoCosto { get; set; }
        public string? MaquinaId { get; set; }
        public decimal? MontoMinimo { get; set; }
        public decimal? MontoMaximo { get; set; }
        public string? Proveedor { get; set; }
    }
}
