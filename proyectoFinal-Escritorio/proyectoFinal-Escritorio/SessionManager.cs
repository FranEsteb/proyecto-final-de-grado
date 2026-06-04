using System.Net.Http;

namespace proyectoFinal_Escritorio
{
    // Esta clase guarda el token de login del usuario para usarlo en todas las llamadas a la API.
    // Cuando el usuario hace login, guardo su token aquí, y cuando hace logout lo borro.
    public static class SessionManager
    {
        // Aquí guardo el token que me da la API cuando el usuario hace login correctamente.
        // Si es null significa que no hay nadie logueado.
        public static string? AuthToken { get; set; }

        // Guardo el nombre del usuario actual para mostrarlo en la interfaz
        public static string? CurrentUsername { get; set; }

        // Guardo el rol del usuario actual para verificar permisos
        public static string? CurrentRole { get; set; }

        // Método para guardar el token cuando el usuario hace login exitoso.
        public static void SetAuthToken(string token)
        {
            AuthToken = token;
        }

        // Método para guardar el nombre del usuario actual
        public static void SetCurrentUsername(string username)
        {
            CurrentUsername = username;
        }

        // Método para guardar el rol del usuario actual
        public static void SetCurrentRole(string role)
        {
            CurrentRole = role;
        }

        // Método para obtener el nombre del usuario actual
        public static string? GetCurrentUsername()
        {
            return CurrentUsername;
        }

        // Método para obtener el rol del usuario actual
        public static string? GetCurrentRole()
        {
            return CurrentRole;
        }
        
        // Método para borrar el token cuando el usuario hace logout.
        // Pongo el token en null para indicar que ya no hay nadie logueado.
        public static void ClearAuthToken()
        {
            AuthToken = null;
            CurrentUsername = null;
            CurrentRole = null;
        }
        
        // Método para obtener el token actual. Si no hay token devuelve string vacío
        // en lugar de null para evitar errores.
        public static string GetAuthToken()
        {
            return AuthToken ?? "";
        }
        
        // Este método crea un HttpClient que ya viene configurado con el token de autenticación.
        // Así no tengo que configurar manualmente el header Authorization en cada llamada a la API.
        public static HttpClient CreateAuthenticatedHttpClient()
        {
            var client = new HttpClient();
            
            // Si hay un token guardado, se lo añado al header Authorization
            if (!string.IsNullOrEmpty(AuthToken))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AuthToken}");
            }
            return client;
        }
    }
}