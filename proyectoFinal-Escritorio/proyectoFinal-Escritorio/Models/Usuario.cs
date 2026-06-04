using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace proyectoFinal_Escritorio.Models
{
    // Clase para manejar información de membresía
    public class Membresia
    {
        [JsonProperty("activa")]
        public bool Activa { get; set; }

        [JsonProperty("tipo")]
        public string Tipo { get; set; } = "basica";

        [JsonProperty("fechaInicio")]
        public DateTime? FechaInicio { get; set; }

        [JsonProperty("fechaFin")]
        public DateTime? FechaFin { get; set; }

        [JsonProperty("descuentoPorPermanencia")]
        public double DescuentoPorPermanencia { get; set; }

        [JsonProperty("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    // Clase principal Usuario
    public class Usuario
    {
        [JsonProperty("_id")]
        public string? Id { get; set; }
        public string Dni { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string? Ciudad { get; set; }
        public string? Telefono { get; set; }
        public int Reputacion { get; set; }

        [JsonProperty("descuentoActual")]
        public double? DescuentoActual { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        // Propiedades de membresía
        [JsonProperty("membresia")]
        public Membresia? Membresia { get; set; }

        // Constructores
        public Usuario() { }

        public Usuario(string dni, string nombre, string apellidos, string email, 
                      DateTime fechaNacimiento, string rol = "cliente")
        {
            Dni = dni;
            Nombre = nombre;
            Apellidos = apellidos;
            Email = email;
            FechaNacimiento = fechaNacimiento;
            Rol = rol;
            Reputacion = 0;
        }

        // Propiedades calculadas
        public string NombreCompleto => $"{Nombre} {Apellidos}";

        public int Edad
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - FechaNacimiento.Year;
                if (FechaNacimiento.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        public bool EsMayorDeEdad => Edad >= 18;

        public string RolCapitalizado => CapitalizeFirstLetter(Rol);

        public string CiudadDisplay => Ciudad ?? "No especificada";
        public string TelefonoDisplay => Telefono ?? "No especificado";

        public string FechaRegistroFormateada => CreatedAt.ToString("dd/MM/yyyy");
        
        // Propiedades calculadas de membresía
        public string TipoMembresia => Membresia?.Activa == true ? CapitalizeFirstLetter(Membresia.Tipo) : "Sin membresía";
        
        public string TiempoRestanteMembresia
        {
            get
            {
                if (Membresia?.Activa != true || Membresia?.FechaFin == null) return "N/A";
                
                var diasRestantes = (Membresia.FechaFin.Value - DateTime.Now).Days;
                if (diasRestantes <= 0) return "Expirada";
                
                if (diasRestantes > 30)
                {
                    var meses = diasRestantes / 30;
                    var dias = diasRestantes % 30;
                    return $"{meses}m {dias}d";
                }
                
                return $"{diasRestantes} días";
            }
        }
        
        public string DescuentoPermanencia => Membresia?.DescuentoPorPermanencia.ToString("0") + "%" ?? "0%";

        // Métodos de validación
        public bool IsValidEmail()
        {
            if (string.IsNullOrEmpty(Email)) return false;
            
            var emailRegex = new System.Text.RegularExpressions.Regex(
                @"^[a-zA-Z0-9._-]+@(gmail|outlook|yahoo|hotmail|live|alu\.edu\.gva)\.(com|es|net|org|edu)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return emailRegex.IsMatch(Email);
        }

        public bool IsValidSpanishCity()
        {
            if (string.IsNullOrEmpty(Ciudad)) return true; // Campo opcional

            var ciudadesEspanolas = new List<string>
            {
                "madrid", "barcelona", "valencia", "sevilla", "zaragoza", "málaga", "murcia",
                "palma", "las palmas de gran canaria", "bilbao", "alicante", "córdoba", "valladolid",
                "vigo", "gijón", "hospitalet de llobregat", "vitoria", "granada", "elche", "oviedo",
                "badalona", "cartagena", "terrassa", "jerez de la frontera", "sabadell", "móstoles",
                "santa cruz de tenerife", "pamplona", "almería", "burgos", "albacete", "getafe",
                "santander", "castellón de la plana", "logroño", "badajoz", "huelva", "salamanca",
                "lleida", "tarragona", "león", "cádiz", "dos hermanas", "marbella", "ourense",
                "torrejón de ardoz", "parla", "alcorcón", "reus", "telde", "lugo", "santiago de compostela",
                "cáceres", "lorca", "coslada", "talavera de la reina", "el puerto de santa maría",
                "cornellà de llobregat", "avilés", "palencia", "gava", "algeciras", "alcalá de guadaíra"
            };

            return ciudadesEspanolas.Contains(Ciudad.ToLower().Trim());
        }

        public List<string> ValidateUser()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(Dni))
                errors.Add("El DNI es obligatorio");

            if (string.IsNullOrEmpty(Nombre))
                errors.Add("El nombre es obligatorio");

            if (string.IsNullOrEmpty(Apellidos))
                errors.Add("Los apellidos son obligatorios");

            if (string.IsNullOrEmpty(Email))
                errors.Add("El email es obligatorio");
            else if (!IsValidEmail())
                errors.Add("El formato del correo no es válido. Debe usar dominios como @gmail.com, @outlook.es, @yahoo.com, o correos corporativos como @alu.edu.gva.es");

            if (!IsValidSpanishCity())
                errors.Add("La ciudad debe ser una ciudad española válida");

            if (FechaNacimiento == default(DateTime))
                errors.Add("La fecha de nacimiento es obligatoria");

            if (string.IsNullOrEmpty(Rol))
                errors.Add("El rol es obligatorio");

            return errors;
        }

        // Método estático para obtener ciudades españolas
        public static List<string> GetCiudadesEspanolas()
        {
            return new List<string>
            {
                "Madrid", "Barcelona", "Valencia", "Sevilla", "Zaragoza", "Málaga", "Murcia",
                "Palma", "Las Palmas de Gran Canaria", "Bilbao", "Alicante", "Córdoba", "Valladolid",
                "Vigo", "Gijón", "Hospitalet de Llobregat", "Vitoria", "Granada", "Elche", "Oviedo",
                "Badalona", "Cartagena", "Terrassa", "Jerez de la Frontera", "Sabadell", "Móstoles",
                "Santa Cruz de Tenerife", "Pamplona", "Almería", "Burgos", "Albacete", "Getafe",
                "Santander", "Castellón de la Plana", "Logroño", "Badajoz", "Huelva", "Salamanca",
                "Lleida", "Tarragona", "León", "Cádiz", "Dos Hermanas", "Marbella", "Ourense",
                "Torrejón de Ardoz", "Parla", "Alcorcón", "Reus", "Telde", "Lugo", "Santiago de Compostela",
                "Cáceres", "Lorca", "Coslada", "Talavera de la Reina", "El Puerto de Santa María",
                "Cornellà de Llobregat", "Avilés", "Palencia", "Gava", "Algeciras", "Alcalá de Guadaíra"
            };
        }

        // Método estático para obtener roles disponibles
        public static List<string> GetRolesDisponibles()
        {
            return new List<string> { "administrador", "empleado", "cliente" };
        }

        // Métodos de utilidad
        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        // Override ToString para debugging
        public override string ToString()
        {
            return $"{NombreCompleto} ({Email}) - {RolCapitalizado}";
        }
    }

    // ViewModel para la interfaz
    public class UsuarioViewModel : INotifyPropertyChanged
    {
        public Usuario OriginalUser { get; }

        public UsuarioViewModel(Usuario usuario)
        {
            OriginalUser = usuario ?? throw new ArgumentNullException(nameof(usuario));
        }

        // Propiedades que exponen los datos del usuario
        public string? Id => OriginalUser.Id;
        public string Dni => OriginalUser.Dni;
        public string Nombre => OriginalUser.Nombre;
        public string Apellidos => OriginalUser.Apellidos;
        public string NombreCompleto => OriginalUser.NombreCompleto;
        public string Email => OriginalUser.Email;
        public string Rol => OriginalUser.Rol;
        public string RolCapitalizado => OriginalUser.RolCapitalizado;
        public DateTime FechaNacimiento => OriginalUser.FechaNacimiento;
        public string Ciudad => OriginalUser.CiudadDisplay;
        public string Telefono => OriginalUser.TelefonoDisplay;
        public int Reputacion => OriginalUser.Reputacion;
        public DateTime CreatedAt => OriginalUser.CreatedAt;
        public string FechaRegistroFormateada => OriginalUser.FechaRegistroFormateada;
        public int Edad => OriginalUser.Edad;
        public bool EsMayorDeEdad => OriginalUser.EsMayorDeEdad;
        public string TipoMembresia => OriginalUser.TipoMembresia;
        public string TiempoRestanteMembresia => OriginalUser.TiempoRestanteMembresia;
        public string DescuentoPermanencia => OriginalUser.DescuentoPermanencia;
        public double? DescuentoActual => OriginalUser.DescuentoActual;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Enum para modos del formulario
    public enum UserFormMode
    {
        Create,
        View,
        Edit
    }
}