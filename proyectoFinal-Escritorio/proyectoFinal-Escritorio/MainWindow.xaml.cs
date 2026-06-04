using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace proyectoFinal_Escritorio
{
    // Esta es la ventana principal que se abre después del login.
    // Desde aquí el usuario puede navegar a todas las secciones del sistema.
    public partial class MainWindow : Window
    {
        // Guardo que botón de la barra lateral está activo para darle un color diferente.
        // Así el usuario sabe en qué sección del sistema se encuentra.
        private Button? _currentActiveButton;

        public MainWindow()
        {
            InitializeComponent();
            // Empiezo mostrando el Dashboard como página principal
            _currentActiveButton = DashboardButton;
            
            // Permito que el usuario pueda arrastrar la ventana haciendo clic en cualquier parte
            this.MouseLeftButtonDown += (sender, e) => {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            };
        }

        // Cuando el usuario hace clic en Dashboard, muestro el dashboard y marco el botón como activo
        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
            SetActiveButton(DashboardButton);
        }

        // Para los módulos que ya están terminados, abro ventanas nuevas.
        // Para los que aún no están listos, muestro un mensaje de "próximamente".
        
        private void UsuariosButton_Click(object sender, RoutedEventArgs e)
        {
            // Abro la ventana de gestión de usuarios como una ventana modal
            var usuariosWindow = new UsuariosWindow();
            usuariosWindow.ShowDialog();
        }

        private void MaquinasButton_Click(object sender, RoutedEventArgs e)
        {
            // Abro la ventana de gestión de máquinas
            var maquinasWindow = new MaquinasWindow();
            maquinasWindow.ShowDialog();
        }

        private void AveriasButton_Click(object sender, RoutedEventArgs e)
        {
            // Abro la ventana simplificada de gestión de averías
            var averiasWindow = new AveriasSimpleWindow();
            averiasWindow.ShowDialog();
        }

        private void ReputacionButton_Click(object sender, RoutedEventArgs e)
        {
            // Abro la ventana del sistema de reputación
            var reputationWindow = new ReputationWindow();
            reputationWindow.ShowDialog();
            SetActiveButton(ReputacionButton);
        }

        private void ProveedoresButton_Click(object sender, RoutedEventArgs e)
        {
            // Abro la ventana de gestión de proveedores
            var proveedoresWindow = new ProveedoresWindow();
            proveedoresWindow.ShowDialog();
        }



        private void CostosButton_Click(object sender, RoutedEventArgs e)
        {
            // Abro la ventana de control de costos
            var costosWindow = new CostosWindow();
            costosWindow.ShowDialog();
        }

        private void ClasesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowModule("Reserva de Clases", "Sistema de inscripción y gestión de clases y actividades");
            SetActiveButton(ClasesButton);
        }

        private void DescuentosButton_Click(object sender, RoutedEventArgs e)
        {
            // Abro la ventana de gestión de descuentos por permanencia
            var descuentosWindow = new DescuentosPermanenciaWindow();
            descuentosWindow.ShowDialog();
            SetActiveButton(DescuentosButton);
        }

        private void MovilButton_Click(object sender, RoutedEventArgs e)
        {
            ShowModule("Aplicación Móvil", "Configuración y gestión de la aplicación móvil para clientes");
            SetActiveButton(MovilButton);
        }

        // Estas funciones hacen que las tarjetas del dashboard funcionen igual que los botones de la barra lateral
        
        private void UsuariosCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Cuando hacen clic en la tarjeta, llamo al mismo método que el botón
            UsuariosButton_Click(sender, new RoutedEventArgs());
        }

        private void MaquinasCard_Click(object sender, MouseButtonEventArgs e)
        {
            MaquinasButton_Click(sender, new RoutedEventArgs());
        }

        private void AveriasCard_Click(object sender, MouseButtonEventArgs e)
        {
            AveriasButton_Click(sender, new RoutedEventArgs());
        }

        private void ReputacionCard_Click(object sender, MouseButtonEventArgs e)
        {
            ReputacionButton_Click(sender, new RoutedEventArgs());
        }

        private void ProveedoresCard_Click(object sender, MouseButtonEventArgs e)
        {
            ProveedoresButton_Click(sender, new RoutedEventArgs());
        }

        private void CostosCard_Click(object sender, MouseButtonEventArgs e)
        {
            CostosButton_Click(sender, new RoutedEventArgs());
        }

        private void ClasesCard_Click(object sender, MouseButtonEventArgs e)
        {
            ClasesButton_Click(sender, new RoutedEventArgs());
        }

        private void DescuentosCard_Click(object sender, MouseButtonEventArgs e)
        {
            DescuentosButton_Click(sender, new RoutedEventArgs());
        }

        private void MovilCard_Click(object sender, MouseButtonEventArgs e)
        {
            MovilButton_Click(sender, new RoutedEventArgs());
        }

        // Estos métodos controlan qué se muestra en la pantalla principal
        
        private void ShowDashboard()
        {
            // Muestro el dashboard y oculto la pantalla de módulos
            DashboardContent.Visibility = Visibility.Visible;
            ModuleContent.Visibility = Visibility.Collapsed;
            
            // Cambio el texto del encabezado para que diga "Dashboard"
            CurrentModuleText.Text = "Dashboard";
            CurrentModuleDescription.Text = "Vista general del sistema";
        }

        private void ShowModule(string moduleTitle, string moduleDescription)
        {
            // Oculto el dashboard y muestro la pantalla de "próximamente"
            DashboardContent.Visibility = Visibility.Collapsed;
            ModuleContent.Visibility = Visibility.Visible;
            
            // Cambio el texto del encabezado con el nombre del módulo
            CurrentModuleText.Text = moduleTitle;
            CurrentModuleDescription.Text = moduleDescription;
            
            // Personalizo el mensaje de "próximamente" con el nombre y descripción del módulo
            ModulePlaceholderTitle.Text = moduleTitle;
            ModulePlaceholderDescription.Text = "Esta funcionalidad estará disponible próximamente.\nAquí se implementará: " + moduleDescription;
        }

        private void BackToDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
            SetActiveButton(DashboardButton);
        }

        private void SetActiveButton(Button activeButton)
        {
            // Cambio el color del botón anterior para que vuelva al color normal
            if (_currentActiveButton != null)
            {
                _currentActiveButton.Style = (Style)FindResource("SidebarButtonStyle");
            }
            
            // Le pongo el color "activo" al botón que acaban de presionar
            activeButton.Style = (Style)FindResource("ActiveSidebarButtonStyle");
            _currentActiveButton = activeButton;
        }

        // Acciones de la barra superior
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Pregunto al usuario si realmente quiere cerrar sesión
            var result = MessageBox.Show(
                "¿Estás seguro de que deseas cerrar sesión?", 
                "Cerrar Sesión", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                // Borro el token de autenticación para cerrar la sesión
                SessionManager.ClearAuthToken();
                
                // Abro la ventana de login y cierro esta ventana
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        // Este método permite que la ventana de login me pase los datos del usuario
        // para mostrarlos en la esquina superior derecha
        public void SetUserInfo(string userName, string userRole)
        {
            UserNameText.Text = userName;
            UserRoleText.Text = userRole;
        }

        // Gestión del estado de la ventana
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Esto arregla un problema con las ventanas maximizadas en algunos sistemas
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.WindowState = WindowState.Maximized;
            }
        }
    }
}