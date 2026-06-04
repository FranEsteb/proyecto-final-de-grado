using System;
using System.Windows;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio.Helpers
{
    /// <summary>
    /// Clase helper para estandarizar todos los diálogos y mensajes de la aplicación.
    /// Elimina duplicación de código de MessageBox.Show en toda la aplicación.
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// Muestra un mensaje de error genérico
        /// </summary>
        public static void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Muestra un error de conexión con formato estándar
        /// </summary>
        public static void ShowConnectionError(Exception ex, string context)
        {
            MessageBox.Show(
                $"Error al {context}: {ex.Message}",
                "Error de Conexión",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// Muestra un error de API con formato estándar
        /// </summary>
        public static void ShowApiError(string errorMessage, string context)
        {
            MessageBox.Show(
                $"Error al {context}: {errorMessage}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// Muestra un error de API desde ApiResponse
        /// </summary>
        public static void ShowApiError<T>(ApiResponse<T> result, string context)
        {
            ShowApiError(result.ErrorMessage ?? "Error desconocido", context);
        }

        /// <summary>
        /// Muestra un mensaje de éxito
        /// </summary>
        public static void ShowSuccess(string message, string title = "Éxito")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Muestra un mensaje de advertencia
        /// </summary>
        public static void ShowWarning(string message, string title = "Advertencia")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Muestra un diálogo de confirmación de eliminación
        /// </summary>
        public static bool ConfirmDelete(string itemName, string itemType = "elemento")
        {
            var result = MessageBox.Show(
                $"¿Estás seguro de que deseas eliminar {itemType} '{itemName}'?\n\nEsta acción no se puede deshacer.",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Muestra un diálogo de confirmación genérico
        /// </summary>
        public static bool Confirm(string message, string title = "Confirmar")
        {
            var result = MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Muestra errores de validación en un formato estándar
        /// </summary>
        public static void ShowValidationErrors(System.Collections.Generic.List<string> errors)
        {
            if (errors == null || errors.Count == 0)
                return;

            var message = "Por favor corrija los siguientes errores:\n\n" + string.Join("\n", errors);
            MessageBox.Show(message, "Errores de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Muestra un mensaje de campo obligatorio
        /// </summary>
        public static void ShowRequiredField(string fieldName, System.Windows.Controls.Control control = null)
        {
            MessageBox.Show(
                $"{fieldName} es obligatorio",
                "Campo Obligatorio",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            control?.Focus();
        }

        /// <summary>
        /// Muestra un mensaje de formato inválido
        /// </summary>
        public static void ShowInvalidFormat(string fieldName, string expectedFormat = null, System.Windows.Controls.Control control = null)
        {
            var message = $"El formato de {fieldName} no es válido";
            if (!string.IsNullOrEmpty(expectedFormat))
            {
                message += $"\n\nFormato esperado: {expectedFormat}";
            }

            MessageBox.Show(message, "Formato Incorrecto", MessageBoxButton.OK, MessageBoxImage.Warning);
            control?.Focus();
        }
    }
}
