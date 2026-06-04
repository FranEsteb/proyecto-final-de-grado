using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Newtonsoft.Json;
using System.Windows.Input;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    // Esta es la ventana de login donde el usuario introduce su email y contraseña.
    // Valida los datos, los envía a la API y si todo está bien abre la ventana principal.
    public partial class LoginWindow : Window
    {
        // Servicio que maneja todas las llamadas a la API
        private readonly ApiService _apiService;

        public LoginWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            
            // Pongo una contraseña por defecto para las pruebas (en producción se quitaría)
            PasswordBox.Password = "";
            
            // Como quité la barra de título para que se vea más moderna, permito arrastrar 
            // la ventana haciendo clic en cualquier parte de ella
            this.MouseLeftButtonDown += (sender, e) => this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Si cierran la ventana de login, cierro toda la aplicación
            Application.Current.Shutdown();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Extraigo y limpio los datos del formulario
            // He usado Trim() para eliminar espacios en blanco accidentales
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Validaciones del lado cliente antes de hacer la llamada a la API
            // He implementado validaciones tempranas para mejorar la UX
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowMessage("Por favor, complete todos los campos.", true);
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowMessage("Por favor, ingrese un email válido.", true);
                return;
            }

            // Activo el estado de carga para dar feedback visual al usuario
            SetLoadingState(true);

            // Llamo al servicio de API para hacer el login
            var loginResult = await _apiService.LoginAsync(email, password);
            
            if (loginResult.Success)
            {
                // Guardo el token y actualizo la autenticación del servicio
                SessionManager.SetAuthToken(loginResult.Data?.Token ?? string.Empty);
                _apiService.UpdateAuthToken();

                // Extraigo la información del usuario del token
                var userInfo = DecodeJwtToken(loginResult.Data?.Token ?? string.Empty);

                // Guardo el rol del usuario para usar en verificación de permisos
                SessionManager.SetCurrentRole(userInfo.Rol);
                    
                // Verifico si el rol del usuario puede acceder a esta aplicación
                if (!IsRoleAllowed(userInfo.Rol))
                {
                    string denialMessage = userInfo.Rol.ToLower() == "cliente"
                        ? "❌ No tienes permisos para acceder. Solo administradores y empleados pueden usar este sistema."
                        : "❌ Acceso denegado. Tu rol no tiene permisos para acceder a este sistema.";
                    ShowMessage(denialMessage, true);
                    SetLoadingState(false);
                    return;
                }
                
                // Muestro mensaje de éxito y preparo la transición
                ShowMessage($"✅ Inicio de sesión exitoso", false);
                await Task.Delay(1000);
                
                // Muestro mensaje de bienvenida con los datos del usuario
                var confirmMessage = $"Bienvenido/a\n\n" +
                                   $"Email: {userInfo.Email}\n" +
                                   $"Rol: {CapitalizeFirstLetter(userInfo.Rol)}\n\n" +
                                   $"¡Inicio de sesión exitoso!";
                
                MessageBox.Show(confirmMessage, "Sistema de Gestión", 
                              MessageBoxButton.OK, MessageBoxImage.Information);

                // Abro la ventana principal con los datos del usuario
                var mainWindow = new MainWindow();
                mainWindow.SetUserInfo(userInfo.Email, CapitalizeFirstLetter(userInfo.Rol));
                mainWindow.Show();
                this.Close();
            }
            else
            {
                // Si hay error, muestro el mensaje de error del servicio
                ShowMessage($"❌ {loginResult.ErrorMessage}", true);
            }
            
            // Siempre quito el estado de carga al final
            SetLoadingState(false);
        }

        // Método para validar formato de email usando .NET Mail API
        // He elegido esta aproximación por ser más robusta que regex
        private bool IsValidEmail(string email)
        {
            try
            {
                // Uso MailAddress para validación, comparando que no cambie el email
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                // Cualquier excepción significa formato inválido
                return false;
            }
        }

        // Método helper para mostrar mensajes de estado al usuario
        // He centralizado esta lógica para mantener consistencia visual
        private void ShowMessage(string message, bool isError)
        {
            StatusTextBlock.Text = message;
            // Uso colores semánticos: rojo para errores, verde para éxito
            // He elegido los colores de la paleta de Microsoft para consistencia
            StatusTextBlock.Foreground = isError ? 
                new SolidColorBrush(Color.FromRgb(232, 17, 35)) :  // Rojo Microsoft
                new SolidColorBrush(Color.FromRgb(16, 124, 16));   // Verde Microsoft
            StatusTextBlock.Visibility = Visibility.Visible;
        }

        // Método para controlar el estado de carga de la UI
        // He implementado una lógica que deshabilita toda interacción durante el loading
        // para prevenir múltiples llamadas simultáneas a la API
        private void SetLoadingState(bool isLoading)
        {
            // Muestro/oculto el indicador de carga
            LoadingGrid.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            
            // Deshabilito todos los controles interactivos durante la carga
            // para evitar acciones concurrentes que podrían causar problemas
            EmailTextBox.IsEnabled = !isLoading;
            PasswordBox.IsEnabled = !isLoading;
            CloseButton.IsEnabled = !isLoading;
            LoginButton.IsEnabled = !isLoading;
        }

        // Esta función saca la información del usuario del token JWT que me envía la API.
        // Un JWT tiene tres partes separadas por puntos, yo necesito la del medio que tiene los datos.
        private UserInfo DecodeJwtToken(string? token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    throw new ArgumentException("Token is null or empty");
                
                // Separo el token por puntos, debe tener exactamente 3 partes
                var parts = token.Split('.');
                if (parts.Length != 3)
                    throw new ArgumentException("Invalid JWT token");

                // Me quedo solo con la parte del medio que tiene la información del usuario
                var payload = parts[1];
                
                // Arreglo el formato Base64 si le faltan caracteres de relleno
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                // Decodifico de Base64 a bytes y luego a string JSON
                var bytes = Convert.FromBase64String(payload);
                var json = Encoding.UTF8.GetString(bytes);
                
                // Deserializo el JSON a mi objeto UserInfo con fallback por seguridad
                return JsonConvert.DeserializeObject<UserInfo>(json) ?? new UserInfo
                {
                    Email = EmailTextBox.Text.Trim(),
                    Rol = "usuario"
                };
            }
            catch
            {
                // Si falla la decodificación, creo un UserInfo básico como fallback
                // He preferido no fallar completamente sino proveer datos mínimos
                return new UserInfo 
                { 
                    Email = EmailTextBox.Text.Trim(),
                    Rol = "usuario" 
                };
            }
        }

        // Esta función verifica si el rol del usuario puede usar esta aplicación.
        // Solo permito administradores y empleados, cualquier otro rol es rechazado.
        private bool IsRoleAllowed(string role)
        {
            if (string.IsNullOrEmpty(role))
                return false;
            
            // Normalizo el rol a minúsculas y elimino espacios para comparación robusta
            var normalizedRole = role.ToLower().Trim();
            
            // Solo permito administradores y empleados acceder al sistema de escritorio
            // He restringido el acceso porque esta aplicación es para operaciones internas
            return normalizedRole == "administrador" || normalizedRole == "empleado";
        }

        // Método helper para formatear strings con la primera letra en mayúscula
        // He creado esto para mejorar la presentación visual de los roles en la UI
        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            // Convierto la primera letra a mayúscula y el resto a minúscula
            // para mantener un formato consistente en toda la aplicación
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        // Limpio los recursos cuando se cierra la ventana
        protected override void OnClosed(EventArgs e)
        {
            // Libero los recursos del servicio de API
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }

    // Clases DTO (Data Transfer Object) para el intercambio de datos con la API
    // He definido estas clases para tipificar fuertemente las respuestas JSON
    
    // Respuesta exitosa del endpoint de login
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    // Respuesta de error genérica de la API
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    // Información del usuario extraída del token JWT
    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}
