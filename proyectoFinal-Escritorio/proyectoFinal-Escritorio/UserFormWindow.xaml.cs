using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;
using proyectoFinal_Escritorio.Helpers;
using System.Linq;

namespace proyectoFinal_Escritorio
{
    public partial class UserFormWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly UserFormMode mode;
        private readonly Usuario? existingUser;

        public UserFormWindow(Usuario? user = null, UserFormMode mode = UserFormMode.Create)
        {
            InitializeComponent();
            _apiService = new ApiService();
            _apiService.UpdateAuthToken();
            this.mode = mode;
            this.existingUser = user;

            InitializeCiudadComboBox();
            SetupForm();

            if (user != null)
            {
                LoadUserData(user);
            }

            // Add event handler for date picker to calculate age
            FechaNacimientoDatePicker.SelectedDateChanged += CalculateAge;
        }

        private void InitializeCiudadComboBox()
        {
            CiudadComboBox.Items.Clear();
            CiudadComboBox.Items.Add(""); // Opción vacía
            
            foreach (var ciudad in Usuario.GetCiudadesEspanolas())
            {
                CiudadComboBox.Items.Add(ciudad);
            }
        }

        private void SetupForm()
        {
            switch (mode)
            {
                case UserFormMode.Create:
                    FormTitleText.Text = "Nuevo Usuario";
                    FormDescriptionText.Text = "Complete los campos para crear un nuevo usuario";
                    SaveButton.Content = "Crear Usuario";
                    break;
                    
                case UserFormMode.Edit:
                    FormTitleText.Text = "Editar Usuario";
                    FormDescriptionText.Text = "Modifique los campos necesarios";
                    SaveButton.Content = "Actualizar Usuario";
                    PasswordLabel.Text = "Nueva Contraseña (opcional)";
                    DniTextBox.IsReadOnly = true; // DNI no editable en modo edición
                    DniTextBox.Background = System.Windows.Media.Brushes.LightGray;
                    break;
                    
                case UserFormMode.View:
                    FormTitleText.Text = "Ver Usuario";
                    FormDescriptionText.Text = "Información del usuario (solo lectura)";
                    SaveButton.Visibility = Visibility.Collapsed;
                    SetFormReadOnly(true);
                    break;
            }
        }

        private void SetFormReadOnly(bool isReadOnly)
        {
            DniTextBox.IsReadOnly = isReadOnly;
            NombreTextBox.IsReadOnly = isReadOnly;
            ApellidosTextBox.IsReadOnly = isReadOnly;
            EmailTextBox.IsReadOnly = isReadOnly;
            CiudadComboBox.IsEnabled = !isReadOnly;
            TelefonoTextBox.IsReadOnly = isReadOnly;
            PasswordBox.IsEnabled = !isReadOnly;
            FechaNacimientoDatePicker.IsEnabled = !isReadOnly;
            RolComboBox.IsEnabled = !isReadOnly;
            MembresiaActivaCheckBox.IsEnabled = !isReadOnly;
            UpdateMembresiaFieldsState();
        }

        private void LoadUserData(Usuario user)
        {
            DniTextBox.Text = user.Dni;
            NombreTextBox.Text = user.Nombre;
            ApellidosTextBox.Text = user.Apellidos;
            EmailTextBox.Text = user.Email;
            CiudadComboBox.Text = user.Ciudad ?? "";
            TelefonoTextBox.Text = user.Telefono ?? "";
            FechaNacimientoDatePicker.SelectedDate = user.FechaNacimiento;

            // Set role
            foreach (ComboBoxItem item in RolComboBox.Items)
            {
                if (item.Tag?.ToString()?.Equals(user.Rol, StringComparison.OrdinalIgnoreCase) == true)
                {
                    RolComboBox.SelectedItem = item;
                    break;
                }
            }

            // Load membership data
            if (user.Membresia != null)
            {
                MembresiaActivaCheckBox.IsChecked = user.Membresia.Activa;

                // Set membership type
                foreach (ComboBoxItem item in TipoMembresiaComboBox.Items)
                {
                    if (item.Tag?.ToString()?.Equals(user.Membresia.Tipo, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        TipoMembresiaComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Set membership dates
                if (user.Membresia.FechaInicio.HasValue)
                    FechaInicioMembresiaDatePicker.SelectedDate = user.Membresia.FechaInicio;
                if (user.Membresia.FechaFin.HasValue)
                    FechaFinMembresiaDatePicker.SelectedDate = user.Membresia.FechaFin;
            }

            // Load current discount if user exists
            if (!string.IsNullOrEmpty(user.Dni))
            {
                DescuentoActualTextBox.Text = (user.DescuentoActual ?? 0).ToString("F0");
            }

            CalculateAge(null, null);
            UpdateMembresiaFieldsState();
        }

        private void CalculateAge(object? sender, SelectionChangedEventArgs? e)
        {
            if (FechaNacimientoDatePicker.SelectedDate.HasValue)
            {
                var birthDate = FechaNacimientoDatePicker.SelectedDate.Value;
                var today = DateTime.Today;
                var age = today.Year - birthDate.Year;
                if (birthDate.Date > today.AddYears(-age)) age--;

                EdadDisplayText.Text = $"Edad: {age} años";
                EsMayorEdadText.Text = age >= 18 ? "Estado: Mayor de edad" : "Estado: Menor de edad";
                EsMayorEdadText.Foreground = age >= 18 ? 
                    System.Windows.Media.Brushes.Green : 
                    System.Windows.Media.Brushes.Orange;
            }
            else
            {
                EdadDisplayText.Text = "Edad: No calculada";
                EsMayorEdadText.Text = "Estado: No calculado";
                EsMayorEdadText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private bool ValidateForm()
        {
            var errors = new StringBuilder();
            string error;

            // Validar DNI
            if (!ValidationHelper.ValidateDni(DniTextBox.Text, out error))
                errors.AppendLine(error);

            // Validar Nombre (solo letras)
            if (!ValidationHelper.ValidateNameField(NombreTextBox.Text, "El nombre", out error))
                errors.AppendLine(error);

            // Validar Apellidos (solo letras)
            if (!ValidationHelper.ValidateNameField(ApellidosTextBox.Text, "Los apellidos", out error))
                errors.AppendLine(error);

            // Validar Email
            if (!ValidationHelper.ValidateEmail(EmailTextBox.Text, out error))
                errors.AppendLine(error);

            // Validar Contraseña (mínimo 8 caracteres)
            if (mode == UserFormMode.Create)
            {
                if (!ValidationHelper.ValidatePassword(PasswordBox.Password, out error, minLength: 8))
                    errors.AppendLine(error);
            }
            else if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                if (!ValidationHelper.ValidatePassword(PasswordBox.Password, out error, minLength: 8))
                    errors.AppendLine(error);
            }

            // Validar Rol
            if (!ValidationHelper.ValidateComboBoxSelection(RolComboBox.SelectedItem, "un rol", out error))
                errors.AppendLine(error);

            // Validar Teléfono (opcional)
            if (!string.IsNullOrWhiteSpace(TelefonoTextBox.Text))
            {
                if (!ValidationHelper.ValidatePhone(TelefonoTextBox.Text, out error, required: false))
                    errors.AppendLine(error);
            }

            // Validar Ciudad española si se proporciona
            if (!string.IsNullOrWhiteSpace(CiudadComboBox.Text) && !IsValidSpanishCity(CiudadComboBox.Text))
                errors.AppendLine("• La ciudad debe ser una ciudad española válida");

            // Validar Fecha de Nacimiento (administrador/empleado: 18 años; cliente: 16 años)
            var selectedRole = (RolComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var minimumAge = (selectedRole == "administrador" || selectedRole == "empleado") ? 18 : 16;
            if (!ValidationHelper.ValidateMinimumAge(FechaNacimientoDatePicker.SelectedDate, minimumAge, out error))
                errors.AppendLine(error);

            // Validar membresía si está activada
            if (MembresiaActivaCheckBox.IsChecked == true)
            {
                if (TipoMembresiaComboBox.SelectedItem == null)
                    errors.AppendLine("• Debe seleccionar un tipo de membresía");

                if (!ValidationHelper.ValidateMembershipDates(
                    FechaInicioMembresiaDatePicker.SelectedDate,
                    FechaFinMembresiaDatePicker.SelectedDate,
                    out error))
                {
                    errors.AppendLine(error);
                }
            }

            if (errors.Length > 0)
            {
                MessageBox.Show($"Por favor corrija los siguientes errores:\n\n{errors}",
                              "Errores de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool IsValidDni(string dni)
        {
            return ValidationHelper.ValidateDni(dni, out _);
        }

        private bool IsValidEmail(string email)
        {
            var tempUser = new Usuario { Email = email };
            return tempUser.IsValidEmail();
        }

        private bool IsValidSpanishCity(string ciudad)
        {
            var tempUser = new Usuario { Ciudad = ciudad };
            return tempUser.IsValidSpanishCity();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                SaveButton.IsEnabled = false;
                SaveButton.Content = mode == UserFormMode.Create ? "Creando..." : "Actualizando...";

                var user = new Usuario
                {
                    Dni = DniTextBox.Text.Trim(),
                    Nombre = NombreTextBox.Text.Trim(),
                    Apellidos = ApellidosTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim().ToLower(),
                    Rol = ((ComboBoxItem)RolComboBox.SelectedItem).Tag.ToString()!,
                    FechaNacimiento = FechaNacimientoDatePicker.SelectedDate!.Value,
                    Ciudad = string.IsNullOrWhiteSpace(CiudadComboBox.Text) ? null : CiudadComboBox.Text.Trim(),
                    Telefono = string.IsNullOrWhiteSpace(TelefonoTextBox.Text) ? null : TelefonoTextBox.Text.Trim(),
                    // Agregar descuento actual
                    DescuentoActual = double.TryParse(DescuentoActualTextBox.Text, out var descActual) ? descActual : 0
                    // Nota: La reputación no se envía aquí. Se gestiona exclusivamente desde ReputationWindow
                };

                // Add membership data if active
                if (MembresiaActivaCheckBox.IsChecked == true)
                {
                    user.Membresia = new Membresia
                    {
                        Activa = true,
                        Tipo = (TipoMembresiaComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "basica",
                        FechaInicio = FechaInicioMembresiaDatePicker.SelectedDate,
                        FechaFin = FechaFinMembresiaDatePicker.SelectedDate,
                        DescuentoPorPermanencia = 0 // Se calcula automáticamente en el servidor
                    };
                }

                // Only include password if it's provided
                if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    user.Password = PasswordBox.Password;
                }

                // Prepare discount update data if descuento actual changed
                string? motivo = null;
                if (!string.IsNullOrWhiteSpace(MotivoDescuentoTextBox.Text))
                {
                    motivo = MotivoDescuentoTextBox.Text.Trim();
                }

                ApiResponse<object> saveResult;

                if (mode == UserFormMode.Create)
                {
                    saveResult = await _apiService.CreateUserAsync(user);

                    // Si se creó exitosamente y tiene membresía, actualizar el usuario para guardar la membresía
                    if (saveResult.Success && user.Membresia != null)
                    {
                        try
                        {
                            // Si la respuesta contiene el ID, usarlo
                            if (saveResult.Data is string usuarioId && !string.IsNullOrEmpty(usuarioId))
                            {
                                user.Id = usuarioId;
                            }

                            var updateResult = await _apiService.UpdateUserAsync(user);
                            if (!updateResult.Success)
                            {
                                MessageBox.Show($"Advertencia: La membresía no se pudo guardar.\nDetalles: {updateResult.ErrorMessage}",
                                              "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Advertencia: Error al guardar membresía.\nDetalles: {ex.Message}",
                                          "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                else // Edit mode
                {
                    user.Id = existingUser!.Id;
                    saveResult = await _apiService.UpdateUserAsync(user);
                }

                if (saveResult.Success)
                {
                    // Handle discount update if descuento actual was provided
                    if (!string.IsNullOrEmpty(DescuentoActualTextBox.Text) && double.TryParse(DescuentoActualTextBox.Text, out var descuentoValue) && descuentoValue > 0)
                    {
                        try
                        {
                            var descuentoResult = await _apiService.UpdateDescuentoActualAsync(user.Dni, descuentoValue, motivo);
                            if (!descuentoResult.Success)
                            {
                                MessageBox.Show($"Advertencia: El descuento no se pudo actualizar.\nDetalles: {descuentoResult.ErrorMessage}",
                                              "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Advertencia: Error al actualizar descuento.\nDetalles: {ex.Message}",
                                          "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }

                    var successMessage = mode == UserFormMode.Create ?
                        "Usuario creado correctamente." :
                        "Usuario actualizado correctamente.";

                    MessageBox.Show(successMessage, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show($"Error al {(mode == UserFormMode.Create ? "crear" : "actualizar")} usuario:\n{saveResult.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveButton.IsEnabled = true;
                SaveButton.Content = mode == UserFormMode.Create ? "Crear Usuario" : "Actualizar Usuario";
            }
        }

        private void MembresiaActivaCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateMembresiaFieldsState();
        }

        private void MembresiaActivaCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateMembresiaFieldsState();
        }

        private void UpdateMembresiaFieldsState()
        {
            bool membresiaActiva = MembresiaActivaCheckBox.IsChecked == true;
            TipoMembresiaComboBox.IsEnabled = membresiaActiva;
            FechaInicioMembresiaDatePicker.IsEnabled = membresiaActiva;
            FechaFinMembresiaDatePicker.IsEnabled = membresiaActiva;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }
}