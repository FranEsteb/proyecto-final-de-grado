using System;
using System.Globalization;
using System.Text.RegularExpressions;
using proyectoFinal_Escritorio.Models;

namespace proyectoFinal_Escritorio.Helpers
{
    /// <summary>
    /// Clase helper para centralizar todas las validaciones de formularios.
    /// Elimina validaciones duplicadas en toda la aplicación.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Valida que un campo obligatorio no esté vacío
        /// </summary>
        public static bool ValidateRequired(string value, string fieldName, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"• {fieldName} es obligatorio";
                return false;
            }
            error = null;
            return true;
        }

        /// <summary>
        /// Valida el formato de un email
        /// </summary>
        public static bool ValidateEmail(string email, out string error)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                error = "• El email es obligatorio";
                return false;
            }

            // Usar la validación del modelo Usuario
            var tempUser = new Usuario { Email = email };
            if (!tempUser.IsValidEmail())
            {
                error = "• El formato del email no es válido (ej: usuario@ejemplo.com)";
                return false;
            }

            error = null;
            return true;
        }

        private const string DniLetras = "TRWAGMYFPDXBNJZSQVHLCKE";

        /// <summary>
        /// Valida el formato y la letra de control de un DNI español (8 dígitos + letra calculada con número % 23)
        /// </summary>
        public static bool ValidateDni(string dni, out string error)
        {
            if (string.IsNullOrWhiteSpace(dni))
            {
                error = "• El DNI es obligatorio";
                return false;
            }

            if (!Regex.IsMatch(dni, @"^\d{8}[A-Za-z]$"))
            {
                error = "• El formato del DNI no es válido (ej: 12345678Z)";
                return false;
            }

            var numero = int.Parse(dni.Substring(0, 8));
            var letraEsperada = DniLetras[numero % 23];
            var letraIntroducida = char.ToUpper(dni[8]);

            if (letraIntroducida != letraEsperada)
            {
                error = $"• La letra del DNI no es correcta (debería ser '{letraEsperada}')";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida el formato de un teléfono (solo números, máximo 15 dígitos)
        /// </summary>
        public static bool ValidatePhone(string phone, out string error, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                if (required)
                {
                    error = "• El teléfono es obligatorio";
                    return false;
                }
                error = null;
                return true;
            }

            // Validar que solo contenga números
            if (!Regex.IsMatch(phone, @"^\d+$"))
            {
                error = "• El teléfono solo puede contener números";
                return false;
            }

            // Validar longitud máxima (15 dígitos es el estándar internacional)
            if (phone.Length > 15)
            {
                error = "• El teléfono no puede tener más de 15 dígitos";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida el formato básico de un CIF (alfanumérico, 3-20 caracteres)
        /// </summary>
        public static bool ValidateCif(string cif, out string error, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(cif))
            {
                if (required)
                {
                    error = "• El CIF es obligatorio";
                    return false;
                }
                error = null;
                return true;
            }

            // Validar que solo contenga letras y números
            if (!Regex.IsMatch(cif, @"^[A-Za-z0-9]+$"))
            {
                error = "• El CIF solo puede contener letras y números";
                return false;
            }

            // Validar longitud mínima y máxima
            if (cif.Length < 3)
            {
                error = "• El CIF debe tener al menos 3 caracteres";
                return false;
            }

            if (cif.Length > 20)
            {
                error = "• El CIF no puede tener más de 20 caracteres";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que un valor numérico sea válido y mayor que cero
        /// </summary>
        public static bool ValidateNumericPositive(string value, string fieldName, out decimal result, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"• {fieldName} es obligatorio";
                result = 0;
                return false;
            }

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            {
                error = $"• {fieldName} debe ser un número válido";
                return false;
            }

            if (result <= 0)
            {
                error = $"• {fieldName} debe ser mayor que cero";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que un valor numérico sea válido (puede ser cero o negativo)
        /// </summary>
        public static bool ValidateNumeric(string value, string fieldName, out decimal result, out string error, bool required = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    error = $"• {fieldName} es obligatorio";
                    result = 0;
                    return false;
                }
                result = 0;
                error = null;
                return true;
            }

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            {
                error = $"• {fieldName} debe ser un número válido";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que un valor entero sea válido y mayor que cero
        /// </summary>
        public static bool ValidateIntegerPositive(string value, string fieldName, out int result, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"• {fieldName} es obligatorio";
                result = 0;
                return false;
            }

            if (!int.TryParse(value, out result))
            {
                error = $"• {fieldName} debe ser un número entero válido";
                return false;
            }

            if (result <= 0)
            {
                error = $"• {fieldName} debe ser mayor que cero";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que una fecha no sea futura
        /// </summary>
        public static bool ValidateDateNotFuture(DateTime? date, string fieldName, out string error)
        {
            if (!date.HasValue)
            {
                error = $"• {fieldName} es obligatorio";
                return false;
            }

            if (date.Value > DateTime.Now)
            {
                error = $"• {fieldName} no puede ser una fecha futura";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que una fecha sea válida
        /// </summary>
        public static bool ValidateDate(DateTime? date, string fieldName, out string error, bool required = true)
        {
            if (!date.HasValue)
            {
                if (required)
                {
                    error = $"• {fieldName} es obligatorio";
                    return false;
                }
                error = null;
                return true;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que un ComboBox tenga un elemento seleccionado
        /// </summary>
        public static bool ValidateComboBoxSelection(object selectedItem, string fieldName, out string error)
        {
            if (selectedItem == null)
            {
                error = $"• Debe seleccionar {fieldName}";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida la longitud mínima de un texto
        /// </summary>
        public static bool ValidateMinLength(string value, string fieldName, int minLength, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"• {fieldName} es obligatorio";
                return false;
            }

            if (value.Trim().Length < minLength)
            {
                error = $"• {fieldName} debe tener al menos {minLength} caracteres";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida la longitud máxima de un texto
        /// </summary>
        public static bool ValidateMaxLength(string value, string fieldName, int maxLength, out string error, bool required = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    error = $"• {fieldName} es obligatorio";
                    return false;
                }
                error = null;
                return true;
            }

            if (value.Trim().Length > maxLength)
            {
                error = $"• {fieldName} no puede tener más de {maxLength} caracteres";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida un rango numérico
        /// </summary>
        public static bool ValidateRange(decimal value, string fieldName, decimal min, decimal max, out string error)
        {
            if (value < min || value > max)
            {
                error = $"• {fieldName} debe estar entre {min} y {max}";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida una contraseña (mínimo 8 caracteres)
        /// </summary>
        public static bool ValidatePassword(string password, out string error, int minLength = 8)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                error = "• La contraseña es obligatoria";
                return false;
            }

            if (password.Length < minLength)
            {
                error = $"• La contraseña debe tener al menos {minLength} caracteres";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que un número no sea negativo (puede ser cero)
        /// </summary>
        public static bool ValidateNonNegative(string value, string fieldName, out decimal result, out string error, bool required = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    error = $"• {fieldName} es obligatorio";
                    result = 0;
                    return false;
                }
                result = 0;
                error = null;
                return true;
            }

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            {
                error = $"• {fieldName} debe ser un número válido";
                return false;
            }

            if (result < 0)
            {
                error = $"• {fieldName} no puede ser negativo";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que un entero no sea negativo (puede ser cero)
        /// </summary>
        public static bool ValidateIntegerNonNegative(string value, string fieldName, out int result, out string error, bool required = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    error = $"• {fieldName} es obligatorio";
                    result = 0;
                    return false;
                }
                result = 0;
                error = null;
                return true;
            }

            if (!int.TryParse(value, out result))
            {
                error = $"• {fieldName} debe ser un número entero válido";
                return false;
            }

            if (result < 0)
            {
                error = $"• {fieldName} no puede ser negativo";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida un número de serie de máquina (alfanumérico, sin espacios)
        /// </summary>
        public static bool ValidateSerialNumber(string serialNumber, out string error)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                error = "• El número de serie es obligatorio";
                return false;
            }

            if (!Regex.IsMatch(serialNumber, @"^[A-Za-z0-9\-]+$"))
            {
                error = "• El número de serie solo puede contener letras, números y guiones";
                return false;
            }

            if (serialNumber.Length < 3)
            {
                error = "• El número de serie debe tener al menos 3 caracteres";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que un valor de reputación esté en el rango correcto (0-100)
        /// </summary>
        public static bool ValidateReputation(string value, out int result, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = 0;
                error = null;
                return true; // La reputación puede ser vacía (se asigna 0 por defecto)
            }

            if (!int.TryParse(value, out result))
            {
                error = "• La reputación debe ser un número entero válido";
                return false;
            }

            if (result < 0 || result > 100)
            {
                error = "• La reputación debe estar entre 0 y 100";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida un porcentaje de descuento (0-100)
        /// </summary>
        public static bool ValidatePercentage(string value, string fieldName, out decimal result, out string error, bool required = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    error = $"• {fieldName} es obligatorio";
                    result = 0;
                    return false;
                }
                result = 0;
                error = null;
                return true;
            }

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            {
                error = $"• {fieldName} debe ser un número válido";
                return false;
            }

            if (result < 0 || result > 100)
            {
                error = $"• {fieldName} debe estar entre 0 y 100";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que un texto solo contenga letras y espacios (no números)
        /// </summary>
        public static bool ValidateNameField(string value, string fieldName, out string error, bool required = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (required)
                {
                    error = $"• {fieldName} es obligatorio";
                    return false;
                }
                error = null;
                return true;
            }

            // Validar que solo contenga letras, espacios y caracteres especiales comunes en nombres (á, é, ñ, etc.)
            if (!Regex.IsMatch(value, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s\-']+$"))
            {
                error = $"• {fieldName} solo puede contener letras";
                return false;
            }

            // Validar que no contenga números
            if (Regex.IsMatch(value, @"\d"))
            {
                error = $"• {fieldName} no puede contener números";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que la edad sea mayor o igual a 16 años (fecha de nacimiento)
        /// </summary>
        public static bool ValidateMinimumAge(DateTime? birthDate, int minimumAge, out string error)
        {
            if (!birthDate.HasValue)
            {
                error = "• La fecha de nacimiento es obligatoria";
                return false;
            }

            if (birthDate.Value > DateTime.Now)
            {
                error = "• La fecha de nacimiento no puede ser futura";
                return false;
            }

            var age = DateTime.Now.Year - birthDate.Value.Year;

            // Ajustar si aún no ha cumplido años este año
            if (birthDate.Value.Date > DateTime.Now.AddYears(-age))
            {
                age--;
            }

            if (age < minimumAge)
            {
                error = $"• El usuario debe tener al menos {minimumAge} años";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida que la fecha de fin sea posterior a la fecha de inicio
        /// </summary>
        public static bool ValidateDateRange(DateTime? startDate, DateTime? endDate, string startFieldName, string endFieldName, out string error)
        {
            if (!startDate.HasValue)
            {
                error = $"• {startFieldName} es obligatoria";
                return false;
            }

            if (!endDate.HasValue)
            {
                error = $"• {endFieldName} es obligatoria";
                return false;
            }

            if (endDate.Value < startDate.Value)
            {
                error = $"• {endFieldName} no puede ser anterior a {startFieldName}";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Valida específicamente las fechas de membresía
        /// </summary>
        public static bool ValidateMembershipDates(DateTime? startDate, DateTime? endDate, out string error)
        {
            return ValidateDateRange(startDate, endDate, "Fecha de inicio", "Fecha de fin", out error);
        }
    }
}
