using System;

namespace proyectoFinal_Escritorio.Services
{
    /// <summary>
    /// Clase auxiliar para verificar permisos de usuario basados en roles
    /// </summary>
    public static class PermissionHelper
    {
        /// <summary>
        /// Verifica si el usuario actual tiene rol de administrador
        /// </summary>
        public static bool IsAdmin()
        {
            var currentRole = SessionManager.GetCurrentRole();
            return currentRole?.ToLower() == "administrador";
        }

        /// <summary>
        /// Verifica si el usuario actual tiene rol de empleado
        /// </summary>
        public static bool IsEmpleado()
        {
            var currentRole = SessionManager.GetCurrentRole();
            return currentRole?.ToLower() == "empleado";
        }

        /// <summary>
        /// Verifica si el usuario actual es administrador o empleado
        /// </summary>
        public static bool IsStaff()
        {
            return IsAdmin() || IsEmpleado();
        }

        /// <summary>
        /// Verifica si el usuario actual tiene permiso para acceder a una sección administrativa
        /// Solo administradores pueden acceder
        /// </summary>
        public static bool CanAccessAdminSection()
        {
            return IsAdmin();
        }

        /// <summary>
        /// Verifica si el usuario actual tiene permiso para eliminar empleados
        /// Solo administradores pueden eliminar empleados
        /// </summary>
        public static bool CanDeleteEmployee()
        {
            return IsAdmin();
        }

        /// <summary>
        /// Verifica si el usuario actual tiene permiso para editar empleados
        /// Solo administradores pueden editar empleados
        /// </summary>
        public static bool CanEditEmployee()
        {
            return IsAdmin();
        }

        /// <summary>
        /// Verifica si un usuario puede editar/eliminar a otro usuario
        /// Los administradores pueden hacer todo
        /// Los empleados no pueden editar/eliminar a administradores
        /// </summary>
        public static bool CanModifyUser(string targetUserRole)
        {
            if (IsAdmin())
                return true;

            // Los empleados no pueden modificar administradores
            if (targetUserRole?.ToLower() == "administrador")
                return false;

            return false; // Los empleados no pueden modificar a nadie
        }

        /// <summary>
        /// Obtiene un mensaje de error de permisos apropiado
        /// </summary>
        public static string GetPermissionDeniedMessage(string action)
        {
            return $"❌ No tienes permisos para {action}. Solo administradores pueden realizar esta acción.";
        }
    }
}
