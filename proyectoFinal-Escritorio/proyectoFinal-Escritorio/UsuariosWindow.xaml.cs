using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using proyectoFinal_Escritorio.Models;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio
{
    public partial class UsuariosWindow : Window
    {
        private readonly ApiService _apiService;
        private ObservableCollection<UsuarioViewModel> allUsers;
        private ObservableCollection<UsuarioViewModel> filteredUsers;

        // Referencia estática a la ventana abierta actualmente para permitir sincronización
        public static UsuariosWindow? CurrentInstance { get; private set; }

        public UsuariosWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _apiService.UpdateAuthToken();
            allUsers = new ObservableCollection<UsuarioViewModel>();
            filteredUsers = new ObservableCollection<UsuarioViewModel>();

            UsersDataGrid.ItemsSource = filteredUsers;

            // Registrar esta instancia como la actualmente abierta
            CurrentInstance = this;

            LoadUsers();
        }

        private async void LoadUsers()
        {
            try
            {
                SetLoadingState(true);
                StatusTextBlock.Text = "Cargando usuarios...";

                var result = await _apiService.GetAllUsersAsync();

                if (result.Success && result.Data != null)
                {
                    var usuarios = result.Data;

                    allUsers.Clear();
                    foreach (var usuario in usuarios)
                    {
                        allUsers.Add(new UsuarioViewModel(usuario));
                    }

                    LoadCiudadesFilter();
                    ApplyCurrentFilters();

                    StatusTextBlock.Text = "Usuarios cargados correctamente";
                    TotalUsersText.Text = $"Total: {allUsers.Count} usuarios";
                }
                else
                {
                    StatusTextBlock.Text = "Error al cargar usuarios";
                    MessageBox.Show($"Error al conectar con el servidor: {result.ErrorMessage}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Error de conexión";
                MessageBox.Show($"Error al cargar usuarios: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void LoadCiudadesFilter()
        {
            var ciudades = allUsers.Where(u => !string.IsNullOrEmpty(u.Ciudad))
                                  .Select(u => u.Ciudad)
                                  .Distinct()
                                  .OrderBy(c => c)
                                  .ToList();

            CiudadFilterComboBox.Items.Clear();
            CiudadFilterComboBox.Items.Add(new ComboBoxItem { Content = "Todas las ciudades" });
            
            foreach (var ciudad in ciudades)
            {
                CiudadFilterComboBox.Items.Add(new ComboBoxItem { Content = ciudad });
            }
            
            CiudadFilterComboBox.SelectedIndex = 0;
        }

        private void ApplyCurrentFilters()
        {
            var filtered = allUsers.AsEnumerable();

            // Filtro por rol
            var rolFilter = ((ComboBoxItem)RolFilterComboBox.SelectedItem)?.Content?.ToString();
            if (rolFilter != "Todos los roles")
            {
                filtered = filtered.Where(u => u.RolCapitalizado.Equals(rolFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por edad
            var edadFilter = ((ComboBoxItem)EdadFilterComboBox.SelectedItem)?.Content?.ToString();
            if (edadFilter == "Mayor de edad (≥18)")
            {
                filtered = filtered.Where(u => u.Edad >= 18);
            }
            else if (edadFilter == "Menor de edad (<18)")
            {
                filtered = filtered.Where(u => u.Edad < 18);
            }

            // Filtro por ciudad
            var ciudadFilter = ((ComboBoxItem)CiudadFilterComboBox.SelectedItem)?.Content?.ToString();
            if (ciudadFilter != "Todas las ciudades")
            {
                filtered = filtered.Where(u => u.Ciudad.Equals(ciudadFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por reputación
            var reputacionFilter = ((ComboBoxItem)ReputacionFilterComboBox.SelectedItem)?.Content?.ToString();
            switch (reputacionFilter)
            {
                case "≥ 80 (Excelente)":
                    filtered = filtered.Where(u => u.Reputacion >= 80);
                    break;
                case "60-79 (Buena)":
                    filtered = filtered.Where(u => u.Reputacion >= 60 && u.Reputacion < 80);
                    break;
                case "40-59 (Regular)":
                    filtered = filtered.Where(u => u.Reputacion >= 40 && u.Reputacion < 60);
                    break;
                case "< 40 (Mala)":
                    filtered = filtered.Where(u => u.Reputacion < 40);
                    break;
            }

            filteredUsers.Clear();
            foreach (var user in filtered.OrderBy(u => u.NombreCompleto))
            {
                filteredUsers.Add(user);
            }

            TotalUsersText.Text = $"Mostrando: {filteredUsers.Count} de {allUsers.Count} usuarios";
        }

        private void SetLoadingState(bool isLoading)
        {
            LoadingProgressBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            RefreshButton.IsEnabled = !isLoading;
            AddUserButton.IsEnabled = !isLoading;
        }

        // Event Handlers
        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allUsers?.Count > 0)
            {
                ApplyCurrentFilters();
            }
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            ApplyCurrentFilters();
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var hasSelection = UsersDataGrid.SelectedItem != null;
            ViewUserButton.IsEnabled = hasSelection;
            EditUserButton.IsEnabled = hasSelection;
            DeleteUserButton.IsEnabled = hasSelection;

            if (hasSelection && UsersDataGrid.SelectedItem is UsuarioViewModel user)
            {
                SelectedUserInfo.Text = $"Usuario seleccionado: {user.NombreCompleto} ({user.RolCapitalizado})";
            }
            else
            {
                SelectedUserInfo.Text = "Selecciona un usuario para ver las acciones disponibles";
            }
        }

        // CRUD Operations
        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            var userForm = new UserFormWindow();
            if (userForm.ShowDialog() == true)
            {
                LoadUsers(); // Reload users after adding
            }
        }

        private void ViewUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is UsuarioViewModel selectedUser)
            {
                var userForm = new UserFormWindow(selectedUser.OriginalUser, UserFormMode.View);
                userForm.ShowDialog();
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is UsuarioViewModel selectedUser)
            {
                var userForm = new UserFormWindow(selectedUser.OriginalUser, UserFormMode.Edit);
                if (userForm.ShowDialog() == true)
                {
                    LoadUsers(); // Reload users after editing
                }
            }
        }

        private async void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is UsuarioViewModel selectedUser)
            {
                var result = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar al usuario {selectedUser.NombreCompleto}?\n\nEsta acción no se puede deshacer.",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        SetLoadingState(true);
                        StatusTextBlock.Text = "Eliminando usuario...";

                        var deleteResult = await _apiService.DeleteUserAsync(selectedUser.Dni);

                        if (deleteResult.Success)
                        {
                            StatusTextBlock.Text = "Usuario eliminado correctamente";
                            MessageBox.Show("Usuario eliminado correctamente.", "Éxito",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadUsers(); // Reload users after deletion
                        }
                        else
                        {
                            StatusTextBlock.Text = "Error al eliminar usuario";
                            MessageBox.Show($"Error al eliminar el usuario: {deleteResult.ErrorMessage}",
                                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusTextBlock.Text = "Error de conexión";
                        MessageBox.Show($"Error al eliminar usuario: {ex.Message}",
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        SetLoadingState(false);
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsers();
        }

        // Método público para refrescar desde otras ventanas
        public void RefreshUserData()
        {
            LoadUsers();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        protected override void OnClosed(EventArgs e)
        {
            // Limpiar la referencia estática cuando se cierra la ventana
            if (CurrentInstance == this)
            {
                CurrentInstance = null;
            }
            _apiService?.Dispose();
            base.OnClosed(e);
        }
    }

}