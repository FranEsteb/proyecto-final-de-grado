using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;

namespace proyectoFinal_Escritorio.Services
{
    // Esta clase centraliza todas las llamadas a la API del backend.
    // En lugar de repetir código HTTP en cada ventana, todo está aquí organizado.
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string API_BASE_URL = "https://localhost:3000/api";

        public ApiService()
        {
            // Acepta el certificado autofirmado generado por el servidor de desarrollo
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (message.RequestUri?.Host == "localhost" || message.RequestUri?.Host == "127.0.0.1")
                        return true;
                    return errors == System.Net.Security.SslPolicyErrors.None;
                }
            };
            _httpClient = new HttpClient(handler);
            // Configuro el token de autenticación si ya existe uno guardado
            UpdateAuthToken();
        }

        // Actualizo el token de autenticación en el cliente HTTP
        public void UpdateAuthToken()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            var token = SessionManager.GetAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }

        // ====== MÉTODOS DE AUTENTICACIÓN ======

        // Hago login con email y contraseña, devuelvo el token si es exitoso
        public async Task<ApiResponse<LoginResponse>> LoginAsync(string email, string password)
        {
            try
            {
                var loginData = new { email = email, password = password };
                var json = JsonConvert.SerializeObject(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{API_BASE_URL}/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseContent);
                    return new ApiResponse<LoginResponse> { Success = true, Data = loginResponse };
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    return new ApiResponse<LoginResponse> 
                    { 
                        Success = false, 
                        ErrorMessage = errorResponse?.Message ?? "Error de autenticación" 
                    };
                }
            }
            catch (HttpRequestException)
            {
                return new ApiResponse<LoginResponse> 
                { 
                    Success = false, 
                    ErrorMessage = "No se pudo conectar al servidor. Verifique que la API esté ejecutándose." 
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<LoginResponse> 
                { 
                    Success = false, 
                    ErrorMessage = $"Error inesperado: {ex.Message}" 
                };
            }
        }

        // ====== MÉTODOS DE USUARIOS ======

        // Obtengo todos los usuarios registrados en el sistema
        public async Task<ApiResponse<List<Usuario>>> GetAllUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:3000/api/usuario/getAll");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Configurar JSON para manejar fechas ISO 8601 correctamente
                    var settings = new JsonSerializerSettings
                    {
                        DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ",
                        DateTimeZoneHandling = DateTimeZoneHandling.Utc
                    };
                    var users = JsonConvert.DeserializeObject<List<Usuario>>(responseContent, settings) ?? new List<Usuario>();
                    return new ApiResponse<List<Usuario>> { Success = true, Data = users };
                }
                else
                {
                    return new ApiResponse<List<Usuario>> 
                    { 
                        Success = false, 
                        ErrorMessage = $"Error al cargar usuarios: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Usuario>> 
                { 
                    Success = false, 
                    ErrorMessage = $"Error de conexión: {ex.Message}" 
                };
            }
        }

        // Creo un nuevo usuario en el sistema
        public async Task<ApiResponse<object>> CreateUserAsync(Usuario user)
        {
            try
            {
                var json = JsonConvert.SerializeObject(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/api/usuario/new", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Intentar extraer el ID del usuario creado de la respuesta
                    try
                    {
                        var responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        var usuarioId = responseData?._id?.ToString() ?? responseData?.id?.ToString();
                        return new ApiResponse<object> { Success = true, Data = usuarioId };
                    }
                    catch
                    {
                        // Si no se puede extraer el ID, retornar solo éxito
                        return new ApiResponse<object> { Success = true };
                    }
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al crear usuario: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Actualizo los datos de un usuario existente
        public async Task<ApiResponse<object>> UpdateUserAsync(Usuario user)
        {
            try
            {
                var json = JsonConvert.SerializeObject(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync("https://localhost:3000/api/usuario/update", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object> 
                    { 
                        Success = false, 
                        ErrorMessage = $"Error al actualizar usuario: {responseContent}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object> 
                { 
                    Success = false, 
                    ErrorMessage = $"Error de conexión: {ex.Message}" 
                };
            }
        }

        // Elimino un usuario del sistema
        public async Task<ApiResponse<object>> DeleteUserAsync(string dni)
        {
            try
            {
                var requestData = new { dni = dni };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Delete, "https://localhost:3000/api/usuario/delete")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al eliminar usuario: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // ====== MÉTODOS DE MÁQUINAS ======

        // Obtengo todas las máquinas registradas en el sistema
        public async Task<ApiResponse<List<Maquina>>> GetAllMachinesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:3000/maquina/getAll");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var machines = JsonConvert.DeserializeObject<List<Maquina>>(responseContent) ?? new List<Maquina>();
                    return new ApiResponse<List<Maquina>> { Success = true, Data = machines };
                }
                else
                {
                    return new ApiResponse<List<Maquina>> 
                    { 
                        Success = false, 
                        ErrorMessage = $"Error al cargar máquinas: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Maquina>> 
                { 
                    Success = false, 
                    ErrorMessage = $"Error de conexión: {ex.Message}" 
                };
            }
        }

        // Creo una nueva máquina en el sistema
        public async Task<ApiResponse<object>> CreateMachineAsync(Maquina machine)
        {
            try
            {
                var json = JsonConvert.SerializeObject(machine);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/maquina/new", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al crear máquina: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Registro una nueva máquina en el sistema (endpoint /register)
        public async Task<ApiResponse<dynamic>> RegisterMachineAsync(object machineData)
        {
            try
            {
                var json = JsonConvert.SerializeObject(machineData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/maquina/register", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<dynamic> { Success = true, Data = responseContent };
                }
                else
                {
                    var errorData = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic>
                    {
                        Success = false,
                        ErrorMessage = responseContent,
                        Data = errorData
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Actualizo los datos de una máquina existente
        public async Task<ApiResponse<object>> UpdateMachineAsync(Maquina machine)
        {
            try
            {
                var json = JsonConvert.SerializeObject(machine);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync("https://localhost:3000/maquina/update", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al actualizar máquina: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Actualizo una máquina con datos dinámicos (endpoint PATCH /update)
        public async Task<ApiResponse<dynamic>> PatchUpdateMachineAsync(object machineData)
        {
            try
            {
                var json = JsonConvert.SerializeObject(machineData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch,
                    "https://localhost:3000/maquina/update")
                {
                    Content = content
                });

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Deserializar la respuesta correctamente
                    var updatedMachine = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic> { Success = true, Data = updatedMachine };
                }
                else
                {
                    var errorData = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic>
                    {
                        Success = false,
                        ErrorMessage = responseContent,
                        Data = errorData
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Elimino una máquina del sistema
        public async Task<ApiResponse<object>> DeleteMachineAsync(string numeroSerie)
        {
            try
            {
                var requestData = new { numeroSerie = numeroSerie };
                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Delete, "https://localhost:3000/maquina/delete")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object> 
                    { 
                        Success = false, 
                        ErrorMessage = $"Error al eliminar máquina: {responseContent}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object> 
                { 
                    Success = false, 
                    ErrorMessage = $"Error de conexión: {ex.Message}" 
                };
            }
        }

        // Exporto datos de máquinas en formato CSV o JSON
        public async Task<ApiResponse<dynamic>> ExportMachinesAsync(string formato)
        {
            try
            {
                var exportData = new 
                { 
                    formato = formato,
                    filtros = new { }
                };

                var json = JsonConvert.SerializeObject(exportData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/maquina/admin/export", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var exportResult = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic> { Success = true, Data = exportResult };
                }
                else
                {
                    return new ApiResponse<dynamic> 
                    { 
                        Success = false, 
                        ErrorMessage = $"Error al exportar datos: {response.StatusCode}" 
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic> 
                { 
                    Success = false, 
                    ErrorMessage = $"Error durante la exportación: {ex.Message}" 
                };
            }
        }

        // ====== MÉTODOS DE COSTOS ======

        // Obtengo todos los gastos asociados a una máquina
        public async Task<ApiResponse<List<Costo>>> GetMachineCostsAsync(string maquinaId)
        {
            try
            {
                // Usar el endpoint específico para obtener gastos por máquina
                var response = await _httpClient.GetAsync($"https://localhost:3000/costo/maquina/{maquinaId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // El endpoint devuelve { gastos: [...], total, cantidad }
                    dynamic result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    var gastos = JsonConvert.DeserializeObject<List<Costo>>(
                        JsonConvert.SerializeObject(result.gastos)) ?? new List<Costo>();

                    return new ApiResponse<List<Costo>> { Success = true, Data = gastos };
                }
                else
                {
                    return new ApiResponse<List<Costo>>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar gastos: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Costo>>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Creo un nuevo registro de costo (sobrecarga 1: con objeto Costo tipado)
        public async Task<ApiResponse<object>> CreateCostoAsync(Costo costo)
        {
            try
            {
                var json = JsonConvert.SerializeObject(costo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/costo/create", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al crear costo: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Creo un nuevo registro de costo (sobrecarga 2: con datos dinámicos, endpoint /new)
        public async Task<ApiResponse<object>> CreateCostoAsync(object costoData)
        {
            try
            {
                var json = JsonConvert.SerializeObject(costoData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/costo/new", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al crear costo: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Actualizo un registro de costo existente
        public async Task<ApiResponse<object>> UpdateCostoAsync(Costo costo)
        {
            try
            {
                var json = JsonConvert.SerializeObject(costo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync("https://localhost:3000/costo/update", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al actualizar costo: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Obtengo estadísticas de gastos para una máquina
        public async Task<ApiResponse<dynamic>> GetCostStatisticsAsync(string maquinaId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://localhost:3000/costo/estadisticas/{maquinaId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var estadisticas = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic> { Success = true, Data = estadisticas };
                }
                else
                {
                    return new ApiResponse<dynamic>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar estadísticas: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // ====== MÉTODOS DE DESCUENTOS POR PERMANENCIA ======

        /// <summary>
        /// Aplica un descuento de permanencia a una membresía de usuario
        /// </summary>
        public async Task<ApiResponse<object>> ApplyDescuentoPermanenciaAsync(string usuarioDni, double porcentaje, string? motivo = null, int duracionDias = 365)
        {
            try
            {
                var request = new
                {
                    usuarioDni = usuarioDni,
                    porcentaje = porcentaje,
                    motivo = motivo ?? "Ajuste manual por permanencia",
                    duracionDias = duracionDias
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    "https://localhost:3000/api/membresia/aplicar-descuento",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true, Data = "Descuento aplicado correctamente" };
                }
                else
                {
                    return new ApiResponse<object> { Success = false, ErrorMessage = $"Error al aplicar descuento: {responseContent}" };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object> { Success = false, ErrorMessage = ex.Message };
            }
        }

        /// <summary>
        /// Actualiza el descuento actual de un usuario (reemplaza el anterior)
        /// </summary>
        public async Task<ApiResponse<object>> UpdateDescuentoActualAsync(string usuarioDni, double descuentoActual, string? motivo = null)
        {
            try
            {
                var request = new
                {
                    Dni = usuarioDni,
                    DescuentoActual = descuentoActual,
                    Motivo = motivo ?? "Actualización manual de descuento"
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync(
                    "https://localhost:3000/api/usuario/updateDescuento",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<object> { Success = true, Data = result };
                }
                else
                {
                    return new ApiResponse<object> { Success = false, ErrorMessage = $"Error al actualizar descuento: {responseContent}" };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object> { Success = false, ErrorMessage = ex.Message };
            }
        }


        // ====== MÉTODOS DE AVERÍAS ======

    
        /// Obtiene todas las averías del sistema
      
        public async Task<ApiResponse<List<Averia>>> GetAllAveriasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:3000/averia/getAll");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var averias = JsonConvert.DeserializeObject<List<Averia>>(responseContent) ?? new List<Averia>();
                    return new ApiResponse<List<Averia>> { Success = true, Data = averias };
                }
                else
                {
                    return new ApiResponse<List<Averia>>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar averías: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Averia>>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

     
        /// Marca una avería como resuelta
      
        public async Task<ApiResponse<object>> UpdateAveriaAsync(Averia averia)
        {
            try
            {
                var json = JsonConvert.SerializeObject(averia);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch,
                    "https://localhost:3000/averia/update")
                {
                    Content = content
                });

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al actualizar avería: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

     
        /// Crea una nueva avería en el sistema
    
        public async Task<ApiResponse<object>> CreateAveriaAsync(Averia averia)
        {
            try
            {
                var json = JsonConvert.SerializeObject(averia);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/averia/new", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al crear avería: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // ====== MÉTODOS DE REPUTACIÓN ======


        /// Obtiene el historial de reputación de un usuario por DNI

        public async Task<ApiResponse<string>> GetReputacionHistoricalAsync(string usuarioDni)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://localhost:3000/reputacion/historial/{usuarioDni}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<string> { Success = true, Data = responseContent };
                }
                else
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar historial de reputación: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

      
        /// Obtiene todos los registros de reputación del sistema
     
        public async Task<ApiResponse<dynamic>> GetAllReputacionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:3000/reputacion/getAll");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var reputacion = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic> { Success = true, Data = reputacion };
                }
                else
                {
                    return new ApiResponse<dynamic>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar reputación: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

     
        /// Crea un nuevo evento de reputación
      
        public async Task<ApiResponse<object>> CreateReputacionAsync(dynamic reputacionData)
        {
            try
            {
                var json = JsonConvert.SerializeObject(reputacionData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/reputacion/new", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al crear evento de reputación: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // ====== MÉTODOS DE MÁQUINAS (Adicionales) ======

        
        /// Obtiene una máquina específica por numeroSerie

        public async Task<ApiResponse<dynamic>> GetMachineByIdAsync(string maquinaId)
        {
            try
            {
                var request = new { numeroSerie = maquinaId };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/maquina/getOne", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var machine = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic> { Success = true, Data = machine };
                }
                else
                {
                    return new ApiResponse<dynamic>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar máquina: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        
        /// Obtiene el historial de uso de una máquina
      
        public async Task<ApiResponse<dynamic>> GetMachineHistoryAsync(string maquinaId)
        {
            try
            {
                var request = new { numeroSerie = maquinaId };
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/maquina/getHistory", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var history = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic> { Success = true, Data = history };
                }
                else
                {
                    return new ApiResponse<dynamic>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar historial: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

       
        /// Obtiene estadísticas de una máquina
       
        public async Task<ApiResponse<dynamic>> GetMachineStatisticsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:3000/maquina/statistics");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var statistics = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic> { Success = true, Data = statistics };
                }
                else
                {
                    return new ApiResponse<dynamic>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar estadísticas: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        
        /// Actualiza el estado de una máquina
        
        public async Task<ApiResponse<object>> UpdateMachineStateAsync(dynamic stateData)
        {
            try
            {
                var json = JsonConvert.SerializeObject(stateData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch,
                    "https://localhost:3000/maquina/updateState")
                {
                    Content = content
                });

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al actualizar estado: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

       
        /// Registra el uso de una máquina
     
        public async Task<ApiResponse<object>> RegisterMachineUsageAsync(dynamic usageData)
        {
            try
            {
                var json = JsonConvert.SerializeObject(usageData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/maquina/registerUsage", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al registrar uso: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // ====== MÉTODOS ADICIONALES (Técnicos, etc) ======

     
        /// Obtiene todos los técnicos activos en el sistema
      
        public async Task<ApiResponse<dynamic>> GetActiveTechnicosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:3000/tecnico/getActivos");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tecnicos = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic> { Success = true, Data = tecnicos };
                }
                else
                {
                    return new ApiResponse<dynamic>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar técnicos: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

      
        /// Obtiene todos los costos del sistema
     
        public async Task<ApiResponse<dynamic>> GetAllCostosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:3000/costo/getAll");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var costos = JsonConvert.DeserializeObject<dynamic>(responseContent);
                    return new ApiResponse<dynamic> { Success = true, Data = costos };
                }
                else
                {
                    return new ApiResponse<dynamic>
                    {
                        Success = false,
                        ErrorMessage = $"Error al cargar costos: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // ====== MÉTODOS DE PROVEEDORES ======

        // Obtengo todos los proveedores registrados
        public async Task<ApiResponse<List<Proveedor>>> GetAllProveedoresAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:3000/proveedor/getAll");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var proveedores = JsonConvert.DeserializeObject<List<Proveedor>>(responseContent) ?? new List<Proveedor>();
                    return new ApiResponse<List<Proveedor>> { Success = true, Data = proveedores };
                }
                else
                {
                    return new ApiResponse<List<Proveedor>>
                    {
                        Success = false,
                        ErrorMessage = "Error al obtener proveedores"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Proveedor>>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Creo un nuevo proveedor
        public async Task<ApiResponse<Proveedor>> CreateProveedorAsync(Proveedor proveedor)
        {
            try
            {
                // Convertir propiedades PascalCase a camelCase para el backend
                var proveedorData = new
                {
                    nombre = proveedor.Nombre,
                    cif = proveedor.Cif,
                    direccion = proveedor.Direccion,
                    telefono = proveedor.Telefono,
                    email = proveedor.Email,
                    productosSuministrados = proveedor.ProductosSuministrados,
                    observaciones = proveedor.Observaciones
                };

                var json = JsonConvert.SerializeObject(proveedorData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/proveedor/new", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var newProveedor = JsonConvert.DeserializeObject<Proveedor>(responseContent);
                    return new ApiResponse<Proveedor> { Success = true, Data = newProveedor };
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    return new ApiResponse<Proveedor>
                    {
                        Success = false,
                        ErrorMessage = errorResponse?.Message ?? "Error al crear proveedor"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<Proveedor>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Actualizo un proveedor existente
        public async Task<ApiResponse<Proveedor>> UpdateProveedorAsync(Proveedor proveedor)
        {
            try
            {
                // El backend actualiza por CIF, no por ID
                var proveedorData = new
                {
                    cif = proveedor.Cif,
                    nombre = proveedor.Nombre,
                    direccion = proveedor.Direccion,
                    telefono = proveedor.Telefono,
                    email = proveedor.Email,
                    productosSuministrados = proveedor.ProductosSuministrados,
                    observaciones = proveedor.Observaciones
                };

                var json = JsonConvert.SerializeObject(proveedorData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync("https://localhost:3000/proveedor/update", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // El backend devuelve solo mensaje, necesitamos devolver el proveedor actualizado
                    return new ApiResponse<Proveedor> { Success = true, Data = proveedor };
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    return new ApiResponse<Proveedor>
                    {
                        Success = false,
                        ErrorMessage = errorResponse?.Message ?? "Error al actualizar proveedor"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<Proveedor>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Elimino un proveedor
        public async Task<ApiResponse<object>> DeleteProveedorAsync(string cif)
        {
            try
            {
                var deleteData = new { cif = cif };
                var json = JsonConvert.SerializeObject(deleteData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Delete, "https://localhost:3000/proveedor/delete")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = errorResponse?.Message ?? "Error al eliminar proveedor"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // ====== MÉTODOS DE PEDIDOS ======

        // Obtengo todos los pedidos
        public async Task<ApiResponse<List<Pedido>>> GetAllPedidosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:3000/pedido/getAll");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var pedidos = JsonConvert.DeserializeObject<List<Pedido>>(responseContent) ?? new List<Pedido>();
                    return new ApiResponse<List<Pedido>> { Success = true, Data = pedidos };
                }
                else
                {
                    return new ApiResponse<List<Pedido>>
                    {
                        Success = false,
                        ErrorMessage = "Error al obtener pedidos"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Pedido>>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Obtengo pedidos de un proveedor específico
        public async Task<ApiResponse<List<Pedido>>> GetPedidosPorProveedorAsync(string proveedorCif)
        {
            try
            {
                var response = await _httpClient.PostAsync("https://localhost:3000/pedido/getByProveedor",
                    new StringContent(JsonConvert.SerializeObject(new { cif = proveedorCif }),
                    Encoding.UTF8, "application/json"));
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var pedidos = JsonConvert.DeserializeObject<List<Pedido>>(responseContent) ?? new List<Pedido>();
                    return new ApiResponse<List<Pedido>> { Success = true, Data = pedidos };
                }
                else
                {
                    return new ApiResponse<List<Pedido>>
                    {
                        Success = false,
                        ErrorMessage = "Error al obtener pedidos del proveedor"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Pedido>>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Creo un nuevo pedido
        public async Task<ApiResponse<Pedido>> CreatePedidoAsync(Pedido pedido)
        {
            try
            {
                // Convertir productos a formato camelCase para el backend
                var productosFormateados = pedido.Productos.Select(p => new
                {
                    producto = p.Producto,
                    cantidad = p.Cantidad,
                    precioUnitario = p.PrecioUnitario
                }).ToList();

                var pedidoData = new
                {
                    numeroPedido = pedido.NumeroPedido,
                    proveedorCif = pedido.ProveedorCif,
                    proveedorNombre = pedido.ProveedorNombre,
                    productos = productosFormateados,
                    estado = pedido.Estado,
                    fechaPedido = pedido.FechaPedido,
                    fechaEntregaEsperada = pedido.FechaEntregaEsperada,
                    observaciones = pedido.Observaciones
                };

                var json = JsonConvert.SerializeObject(pedidoData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://localhost:3000/pedido/new", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var newPedido = JsonConvert.DeserializeObject<Pedido>(responseContent);
                    return new ApiResponse<Pedido> { Success = true, Data = newPedido };
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    return new ApiResponse<Pedido>
                    {
                        Success = false,
                        ErrorMessage = errorResponse?.Message ?? "Error al crear pedido"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<Pedido>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Actualizo un pedido existente
        public async Task<ApiResponse<Pedido>> UpdatePedidoAsync(Pedido pedido)
        {
            try
            {
                var pedidoData = new
                {
                    numeroPedido = pedido.NumeroPedido,
                    estado = pedido.Estado,
                    fechaEntrega = pedido.FechaEntrega,
                    observaciones = pedido.Observaciones
                };

                var json = JsonConvert.SerializeObject(pedidoData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"https://localhost:3000/pedido/update/{pedido.NumeroPedido}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<Pedido> { Success = true, Data = pedido };
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    return new ApiResponse<Pedido>
                    {
                        Success = false,
                        ErrorMessage = errorResponse?.Message ?? "Error al actualizar pedido"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<Pedido>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Elimino un pedido
        public async Task<ApiResponse<object>> DeletePedidoAsync(string numeroPedido)
        {
            try
            {
                var deleteData = new { numeroPedido = numeroPedido };
                var json = JsonConvert.SerializeObject(deleteData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Delete, "https://localhost:3000/pedido/delete")
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = errorResponse?.Message ?? "Error al eliminar pedido"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        /// Elimina un costo del sistema

        public async Task<ApiResponse<object>> DeleteCostoAsync(string costoId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"https://localhost:3000/costo/delete/{costoId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse<object> { Success = true };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        ErrorMessage = $"Error al eliminar costo: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}"
                };
            }
        }

        // Limpio los recursos cuando ya no necesito el servicio
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // ====== CLASES AUXILIARES ======

    // Clase genérica para envolver las respuestas de la API
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Clases DTO para las respuestas de la API (si no existen ya)
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
