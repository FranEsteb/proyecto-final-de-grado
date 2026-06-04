using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Helpers;

namespace proyectoFinal_Escritorio.Models
{
    // Esta clase guarda la información de garantía de una máquina.
    // Calcula automáticamente si la garantía sigue vigente y cuántos días quedan.
    public class Garantia
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? Proveedor { get; set; }

        // Estas propiedades calculan automáticamente si la garantía está vigente y cuántos días quedan
        public bool EstaVigente => FechaFin.HasValue && FechaFin.Value > DateTime.Now;
        public int DiasRestantes => FechaFin.HasValue ? Math.Max(0, (int)(FechaFin.Value - DateTime.Now).TotalDays) : 0;
    }

    public class Especificaciones
    {
        public double? Peso { get; set; }
        public string? Dimensiones { get; set; }
        public double? ConsumoEnergia { get; set; }
        public double? CapacidadMaxima { get; set; }
    }

    public class HistorialEstado
    {
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCambio { get; set; }
        public string? Motivo { get; set; }
        public string? Usuario { get; set; }

        public string FechaFormateada => FechaCambio.ToString("dd/MM/yyyy HH:mm");
    }

    public class Notificaciones
    {
        public bool MantenimientoPendiente { get; set; }
        public bool GarantiaPorVencer { get; set; }
        public bool ReparacionNecesaria { get; set; }

        public bool TieneNotificaciones => MantenimientoPendiente || GarantiaPorVencer || ReparacionNecesaria;
        public int ConteoNotificaciones => (MantenimientoPendiente ? 1 : 0) + (GarantiaPorVencer ? 1 : 0) + (ReparacionNecesaria ? 1 : 0);
    }

    public class Proveedor
    {
        [JsonProperty("_id")]
        public string? Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Cif { get; set; }

        [JsonProperty("productosSuministrados")]
        public List<string> ProductosSuministrados { get; set; } = new List<string>();

        public string? Observaciones { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Propiedades formateadas
        public string CifFormateado => !string.IsNullOrEmpty(Cif) ? Cif : "No especificado";
        public string DireccionFormateada => !string.IsNullOrEmpty(Direccion) ? Direccion : "No especificada";
        public string TelefonoFormateado => !string.IsNullOrEmpty(Telefono) ? Telefono : "No especificado";
        public string EmailFormateado => !string.IsNullOrEmpty(Email) ? Email : "No especificado";
        public string ProductosTexto => ProductosSuministrados.Count > 0
            ? string.Join(", ", ProductosSuministrados)
            : "Sin productos";
        public string FechaRegistroFormateada => CreatedAt.ToString("dd/MM/yyyy");
        public string FechaActualizacionFormateada => UpdatedAt.ToString("dd/MM/yyyy HH:mm");

        // Validación
        public List<string> ValidateProveedor()
        {
            var errors = new List<string>();
            string error;

            // Validar nombre obligatorio (sin números)
            if (!ValidationHelper.ValidateRequired(Nombre, "El nombre del proveedor", out error))
                errors.Add(error);
            else
            {
                // Validar que el nombre no contenga números
                if (!ValidationHelper.ValidateNameField(Nombre, "El nombre del proveedor", out error, required: true))
                    errors.Add(error);
                else if (!ValidationHelper.ValidateMaxLength(Nombre, "El nombre", 150, out error, required: true))
                    errors.Add(error);
            }

            // Validar CIF (opcional pero con formato correcto si se proporciona)
            if (!string.IsNullOrWhiteSpace(Cif))
            {
                if (!ValidationHelper.ValidateCif(Cif, out error, required: false))
                    errors.Add(error);
            }

            // Validar dirección máxima
            if (!ValidationHelper.ValidateMaxLength(Direccion, "La dirección", 250, out error, required: false))
                errors.Add(error);

            // Validar teléfono (opcional pero con formato correcto si se proporciona)
            if (!string.IsNullOrWhiteSpace(Telefono))
            {
                if (!ValidationHelper.ValidatePhone(Telefono, out error, required: false))
                    errors.Add(error);
            }

            // Validar email (opcional pero con formato correcto)
            if (!string.IsNullOrWhiteSpace(Email))
            {
                if (!ValidationHelper.ValidateEmail(Email, out error))
                    errors.Add(error);
                else if (!ValidationHelper.ValidateMaxLength(Email, "El email", 100, out error, required: false))
                    errors.Add(error);
            }

            // Validar observaciones máxima
            if (!ValidationHelper.ValidateMaxLength(Observaciones, "Las observaciones", 500, out error, required: false))
                errors.Add(error);

            return errors;
        }

        public override string ToString()
        {
            return $"{Nombre} ({CifFormateado})";
        }
    }

    // Esta es la clase principal que representa una máquina del gimnasio.
    // Contiene toda la información básica, garantía, especificaciones, historial y notificaciones.
    public class Maquina
    {
        [JsonProperty("_id")]
        public string? Id { get; set; }
        
        public string NumeroSerie { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string? Modelo { get; set; }
        public string? Marca { get; set; }
        
        public DateTime? FechaCompra { get; set; }
        public string Estado { get; set; } = "operativa";
        
        [JsonIgnore]
        public string? ProveedorId { get; set; }

        [JsonProperty("proveedor")]
        public Proveedor? Proveedor { get; set; }
        public string? Ubicacion { get; set; }
        
        public DateTime? MantenimientoProgramado { get; set; }
        public string? ImagenMaquina { get; set; }
        
        public double HorasUso { get; set; }
        public DateTime? UltimoMantenimiento { get; set; }
        public double? CostoCompra { get; set; }
        public double CostoReparacion { get; set; }
        public double CostoMantenimiento { get; set; }
        
        public Garantia? Garantia { get; set; }
        public Especificaciones? Especificaciones { get; set; }
        public List<HistorialEstado> HistorialEstados { get; set; } = new List<HistorialEstado>();
        public Notificaciones? Notificaciones { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Estas propiedades formatean automáticamente los datos para mostrarlos en la interfaz
        public string TipoCapitalizado => CapitalizeFirstLetter(Tipo);
        public string EstadoCapitalizado => CapitalizeFirstLetter(Estado);
        public string MarcaModelo => !string.IsNullOrEmpty(Marca) && !string.IsNullOrEmpty(Modelo) 
            ? $"{Marca} {Modelo}" : (Marca ?? Modelo ?? "No especificado");
        
        public string FechaCompraFormateada => FechaCompra?.ToString("dd/MM/yyyy") ?? "No registrada";
        public string FechaRegistroFormateada => CreatedAt.ToString("dd/MM/yyyy");
        
        public string MantenimientoFormateado => MantenimientoProgramado?.ToString("dd/MM/yyyy") ?? "No programado";
        public string UltimoMantenimientoFormateado => UltimoMantenimiento?.ToString("dd/MM/yyyy") ?? "Nunca";
        
        public string CostoCompraFormateado => CostoCompra?.ToString("C") ?? "No registrado";
        public string CostoReparacionFormateado => CostoReparacion > 0 ? CostoReparacion.ToString("C") : "Sin gastos";
        public string CostoMantenimientoFormateado => CostoMantenimiento > 0 ? CostoMantenimiento.ToString("C") : "Sin gastos";
        public double CostoTotal => CostoReparacion + CostoMantenimiento;
        public string CostoTotalFormateado => CostoTotal > 0 ? CostoTotal.ToString("C") : "Sin gastos";
        public string HorasUsoFormateadas => $"{HorasUso:F1}h";
        
        public string ProveedorNombre => Proveedor?.Nombre ?? "Sin proveedor";
        public string UbicacionDisplay => Ubicacion ?? "No asignada";
        
        public bool RequiereAtencion => Notificaciones?.TieneNotificaciones ?? false;
        public string NotificacionesTexto => Notificaciones?.ConteoNotificaciones.ToString() ?? "0";
        
        public int DiasDesdeCompra => FechaCompra.HasValue ? (int)(DateTime.Now - FechaCompra.Value).TotalDays : 0;
        public int DiasHastaMantenimiento => MantenimientoProgramado.HasValue ? (int)(MantenimientoProgramado.Value - DateTime.Now).TotalDays : int.MaxValue;
        
        public bool MantenimientoVencido => MantenimientoProgramado.HasValue && MantenimientoProgramado.Value < DateTime.Now;
        public bool GarantiaVigente => Garantia?.EstaVigente ?? false;
        
        // Lista con todos los estados posibles que puede tener una máquina
        public static List<string> EstadosDisponibles => new()
        {
            "operativa", "en reparación", "fuera de servicio", "mantenimiento"
        };

        // Lista con todos los tipos de máquinas que maneja el gimnasio
        public static List<string> TiposDisponibles => new()
        {
            "bicicleta", "cinta", "elíptica", "pesas", "remo", "escaladora", "prensa"
        };

        // Constructores
        public Maquina() 
        {
            Notificaciones = new Notificaciones();
        }

        public Maquina(string numeroSerie, string tipo) : this()
        {
            NumeroSerie = numeroSerie;
            Tipo = tipo;
            Estado = "operativa";
            HorasUso = 0;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        // Esta función verifica que todos los datos de la máquina sean correctos
        public List<string> ValidateMachine()
        {
            var errors = new List<string>();
            string error;

            // Validar número de serie (obligatorio, alfanumérico)
            if (!ValidationHelper.ValidateSerialNumber(NumeroSerie, out error))
                errors.Add(error);
            else if (!ValidationHelper.ValidateMaxLength(NumeroSerie, "El número de serie", 50, out error, required: true))
                errors.Add(error);

            // Validar tipo obligatorio
            if (!ValidationHelper.ValidateRequired(Tipo, "El tipo de máquina", out error))
                errors.Add(error);
            else if (!TiposDisponibles.Contains(Tipo.ToLower()))
                errors.Add($"• El tipo debe ser uno de: {string.Join(", ", TiposDisponibles)}");

            // Validar marca máxima (opcional)
            if (!ValidationHelper.ValidateMaxLength(Marca, "La marca", 100, out error, required: false))
                errors.Add(error);

            // Validar modelo máximo (opcional)
            if (!ValidationHelper.ValidateMaxLength(Modelo, "El modelo", 100, out error, required: false))
                errors.Add(error);

            // Validar ubicación máxima (opcional)
            if (!ValidationHelper.ValidateMaxLength(Ubicacion, "La ubicación", 200, out error, required: false))
                errors.Add(error);

            // Validar fecha de compra (OBLIGATORIA y no futura)
            if (!ValidationHelper.ValidateDateNotFuture(FechaCompra, "La fecha de compra", out error))
                errors.Add(error);

            // Validar costo de compra (OBLIGATORIO y no negativo, puede ser 0)
            if (!CostoCompra.HasValue)
                errors.Add("• El costo de compra es obligatorio");
            else if (CostoCompra.Value < 0)
                errors.Add("• El costo de compra no puede ser negativo");

            // Validar último mantenimiento (opcional, pero no puede ser futuro)
            if (UltimoMantenimiento.HasValue)
            {
                if (!ValidationHelper.ValidateDateNotFuture(UltimoMantenimiento, "El último mantenimiento", out error))
                    errors.Add(error);
            }

            // Validar mantenimiento programado (opcional, pero debe ser futuro si existe)
            if (MantenimientoProgramado.HasValue)
            {
                if (MantenimientoProgramado.Value < DateTime.Now)
                    errors.Add("• El mantenimiento programado debe ser una fecha futura");
            }

            // Validar que último mantenimiento sea anterior al próximo mantenimiento
            if (UltimoMantenimiento.HasValue && MantenimientoProgramado.HasValue)
            {
                if (UltimoMantenimiento.Value > MantenimientoProgramado.Value)
                    errors.Add("• El último mantenimiento no puede ser posterior al mantenimiento programado");
            }

            // Validar horas de uso no negativas
            if (HorasUso < 0)
                errors.Add("• Las horas de uso no pueden ser negativas");

            // Validar estado válido
            if (!ValidationHelper.ValidateRequired(Estado, "El estado", out error))
                errors.Add(error);
            else if (!EstadosDisponibles.Contains(Estado.ToLower()))
                errors.Add($"• El estado debe ser uno de: {string.Join(", ", EstadosDisponibles)}");

            return errors;
        }

        // Cuando cambio el estado de una máquina, guardo el cambio en el historial
        public void AgregarAlHistorial(string nuevoEstado, string motivo, string usuario)
        {
            HistorialEstados.Add(new HistorialEstado
            {
                Estado = nuevoEstado,
                FechaCambio = DateTime.Now,
                Motivo = motivo,
                Usuario = usuario
            });
            
            Estado = nuevoEstado;
            UpdatedAt = DateTime.Now;
        }

        // Función auxiliar para poner la primera letra en mayúscula
        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        public override string ToString()
        {
            return $"{NumeroSerie} - {TipoCapitalizado} {MarcaModelo}";
        }
    }

    // Esta clase adapta los datos de la máquina para mostrarlos en las tablas de la interfaz
    public class MaquinaViewModel : INotifyPropertyChanged
    {
        public Maquina OriginalMachine { get; }

        public MaquinaViewModel(Maquina maquina)
        {
            OriginalMachine = maquina ?? throw new ArgumentNullException(nameof(maquina));
        }

        // Estas propiedades conectan los datos de la máquina original con la tabla
        public string? Id => OriginalMachine.Id;
        public string NumeroSerie => OriginalMachine.NumeroSerie;
        public string Tipo => OriginalMachine.Tipo;
        public string TipoCapitalizado => OriginalMachine.TipoCapitalizado;
        public string? Modelo => OriginalMachine.Modelo;
        public string? Marca => OriginalMachine.Marca;
        public string MarcaModelo => OriginalMachine.MarcaModelo;
        public DateTime? FechaCompra => OriginalMachine.FechaCompra;
        public string FechaCompraFormateada => OriginalMachine.FechaCompraFormateada;
        public string Estado => OriginalMachine.Estado;
        public string EstadoCapitalizado => OriginalMachine.EstadoCapitalizado;
        public string? Ubicacion => OriginalMachine.Ubicacion;
        public string UbicacionDisplay => OriginalMachine.UbicacionDisplay;
        public DateTime? MantenimientoProgramado => OriginalMachine.MantenimientoProgramado;
        public string MantenimientoFormateado => OriginalMachine.MantenimientoFormateado;
        public double HorasUso => OriginalMachine.HorasUso;
        public string HorasUsoFormateadas => OriginalMachine.HorasUsoFormateadas;
        public string ProveedorNombre => OriginalMachine.ProveedorNombre;
        public double? CostoCompra => OriginalMachine.CostoCompra;
        public string CostoCompraFormateado => OriginalMachine.CostoCompraFormateado;
        public double CostoReparacion => OriginalMachine.CostoReparacion;
        public double CostoMantenimiento => OriginalMachine.CostoMantenimiento;
        public string CostoReparacionFormateado => OriginalMachine.CostoReparacionFormateado;
        public string CostoMantenimientoFormateado => OriginalMachine.CostoMantenimientoFormateado;
        public string CostoTotalFormateado => (OriginalMachine.CostoReparacion + OriginalMachine.CostoMantenimiento).ToString("C");
        public bool RequiereAtencion => OriginalMachine.RequiereAtencion;
        public string NotificacionesTexto => OriginalMachine.NotificacionesTexto;
        public DateTime CreatedAt => OriginalMachine.CreatedAt;
        public string FechaRegistroFormateada => OriginalMachine.FechaRegistroFormateada;
        public bool MantenimientoVencido => OriginalMachine.MantenimientoVencido;
        public bool GarantiaVigente => OriginalMachine.GarantiaVigente;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Define si el formulario de máquina se usa para crear, ver o editar
    public enum MachineFormMode
    {
        Create,
        View,
        Edit
    }

    // Esta clase guarda todos los criterios de filtrado que puede aplicar el usuario
    public class MaquinaFilter
    {
        public string? Tipo { get; set; }
        public string? Marca { get; set; }
        public string? Estado { get; set; }
        public string? Ubicacion { get; set; }
        public string? Proveedor { get; set; }
        public DateTime? FechaCompraDesde { get; set; }
        public DateTime? FechaCompraHasta { get; set; }
        public bool? RequiereAtencion { get; set; }
        public string? BusquedaTexto { get; set; }
    }

    // ViewModel para proveedores que adapta los datos a la vista
    public class ProveedorViewModel : INotifyPropertyChanged
    {
        public Proveedor OriginalProveedor { get; }

        public ProveedorViewModel(Proveedor proveedor)
        {
            OriginalProveedor = proveedor ?? throw new ArgumentNullException(nameof(proveedor));
        }

        // Propiedades que se enlazan a la UI
        public string? Id => OriginalProveedor.Id;
        public string Nombre => OriginalProveedor.Nombre;
        public string? Cif => OriginalProveedor.Cif;
        public string CifFormateado => OriginalProveedor.CifFormateado;
        public string? Direccion => OriginalProveedor.Direccion;
        public string DireccionFormateada => OriginalProveedor.DireccionFormateada;
        public string? Telefono => OriginalProveedor.Telefono;
        public string TelefonoFormateado => OriginalProveedor.TelefonoFormateado;
        public string? Email => OriginalProveedor.Email;
        public string EmailFormateado => OriginalProveedor.EmailFormateado;
        public List<string> ProductosSuministrados => OriginalProveedor.ProductosSuministrados;
        public string ProductosTexto => OriginalProveedor.ProductosTexto;
        public string? Observaciones => OriginalProveedor.Observaciones;
        public DateTime CreatedAt => OriginalProveedor.CreatedAt;
        public string FechaRegistroFormateada => OriginalProveedor.FechaRegistroFormateada;
        public string FechaActualizacionFormateada => OriginalProveedor.FechaActualizacionFormateada;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Define si el formulario de proveedor se usa para crear, ver o editar
    public enum ProveedorFormMode
    {
        Create,
        View,
        Edit
    }

    // Filtros disponibles para la búsqueda de proveedores
    public class ProveedorFilter
    {
        public string? BusquedaNombre { get; set; }
        public string? BusquedaCif { get; set; }
        public string? BusquedaProducto { get; set; }
    }

    // Modelo para líneas de pedidos (productos con cantidad)
    public class LineaPedido
    {
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public double? PrecioUnitario { get; set; }

        public double Total => Cantidad * (PrecioUnitario ?? 0);
    }

    // Modelo principal para Pedidos
    public class Pedido
    {
        [JsonProperty("_id")]
        public string? Id { get; set; }

        public string NumeroPedido { get; set; } = string.Empty;
        public string ProveedorCif { get; set; } = string.Empty;
        public string ProveedorNombre { get; set; } = string.Empty;

        public List<LineaPedido> Productos { get; set; } = new List<LineaPedido>();

        public string Estado { get; set; } = "pendiente"; // pendiente, confirmado, entregado, cancelado
        public DateTime FechaPedido { get; set; } = DateTime.Now;
        public DateTime? FechaEntregaEsperada { get; set; }
        public DateTime? FechaEntrega { get; set; }

        public string? Observaciones { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Propiedades calculadas y formateadas
        public double Total => Productos.Sum(p => p.Total);
        public string TotalFormateado => Total.ToString("C");
        public int CantidadProductos => Productos.Count;
        public int CantidadTotal => Productos.Sum(p => p.Cantidad);

        public string EstadoCapitalizado => char.ToUpper(Estado[0]) + Estado.Substring(1);
        public string FechaPedidoFormateada => FechaPedido.ToString("dd/MM/yyyy");
        public string FechaEntregaEsperadaFormateada => FechaEntregaEsperada?.ToString("dd/MM/yyyy") ?? "No especificada";
        public string FechaEntregaFormateada => FechaEntrega?.ToString("dd/MM/yyyy") ?? "No entregado";
        public string FechaRegistroFormateada => CreatedAt.ToString("dd/MM/yyyy HH:mm");

        // Validación de pedido
        public List<string> ValidatePedido()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ProveedorCif))
                errors.Add("El CIF del proveedor es obligatorio");

            if (string.IsNullOrWhiteSpace(ProveedorNombre))
                errors.Add("El nombre del proveedor es obligatorio");

            if (Productos == null || Productos.Count == 0)
                errors.Add("Debe agregar al menos un producto al pedido");
            else
            {
                foreach (var producto in Productos)
                {
                    if (string.IsNullOrWhiteSpace(producto.Producto))
                        errors.Add("Todos los productos deben tener nombre");
                    if (producto.Cantidad <= 0)
                        errors.Add("La cantidad de productos debe ser mayor a 0");
                }
            }

            return errors;
        }

        public override string ToString()
        {
            return $"{NumeroPedido} - {ProveedorNombre} ({EstadoCapitalizado})";
        }
    }

    // ViewModel para pedidos
    public class PedidoViewModel : INotifyPropertyChanged
    {
        public Pedido OriginalPedido { get; }

        public PedidoViewModel(Pedido pedido)
        {
            OriginalPedido = pedido ?? throw new ArgumentNullException(nameof(pedido));
        }

        public string? Id => OriginalPedido.Id;
        public string NumeroPedido => OriginalPedido.NumeroPedido;
        public string ProveedorNombre => OriginalPedido.ProveedorNombre;
        public string ProveedorCif => OriginalPedido.ProveedorCif;
        public int CantidadProductos => OriginalPedido.CantidadProductos;
        public int CantidadTotal => OriginalPedido.CantidadTotal;
        public double Total => OriginalPedido.Total;
        public string TotalFormateado => OriginalPedido.TotalFormateado;
        public string Estado => OriginalPedido.Estado;
        public string EstadoCapitalizado => OriginalPedido.EstadoCapitalizado;
        public DateTime FechaPedido => OriginalPedido.FechaPedido;
        public string FechaPedidoFormateada => OriginalPedido.FechaPedidoFormateada;
        public DateTime? FechaEntregaEsperada => OriginalPedido.FechaEntregaEsperada;
        public string FechaEntregaEsperadaFormateada => OriginalPedido.FechaEntregaEsperadaFormateada;
        public DateTime? FechaEntrega => OriginalPedido.FechaEntrega;
        public string FechaEntregaFormateada => OriginalPedido.FechaEntregaFormateada;
        public string? Observaciones => OriginalPedido.Observaciones;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Enum para estados de pedidos
    public enum EstadoPedido
    {
        Pendiente,
        Confirmado,
        Entregado,
        Cancelado
    }
}