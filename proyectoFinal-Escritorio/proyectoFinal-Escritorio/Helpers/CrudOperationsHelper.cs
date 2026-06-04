using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using proyectoFinal_Escritorio.Services;

namespace proyectoFinal_Escritorio.Helpers
{
    /// <summary>
    /// Clase helper para estandarizar operaciones CRUD (Create, Read, Update, Delete).
    /// Elimina código duplicado de operaciones comunes en todas las ventanas.
    /// </summary>
    public class CrudOperationsHelper
    {
        private readonly Action<bool> _setLoadingState;
        private readonly Action<string> _updateStatus;
        private readonly Action _reloadData;

        /// <summary>
        /// Constructor que recibe callbacks para controlar el estado de la UI
        /// </summary>
        /// <param name="setLoadingState">Callback para activar/desactivar estado de carga</param>
        /// <param name="updateStatus">Callback para actualizar texto de estado</param>
        /// <param name="reloadData">Callback para recargar datos después de operación exitosa</param>
        public CrudOperationsHelper(
            Action<bool> setLoadingState,
            Action<string> updateStatus,
            Action reloadData)
        {
            _setLoadingState = setLoadingState ?? throw new ArgumentNullException(nameof(setLoadingState));
            _updateStatus = updateStatus ?? throw new ArgumentNullException(nameof(updateStatus));
            _reloadData = reloadData ?? throw new ArgumentNullException(nameof(reloadData));
        }

        /// <summary>
        /// Ejecuta una operación de eliminación con confirmación y manejo de errores estándar
        /// </summary>
        public async Task<bool> DeleteAsync<TId>(
            TId id,
            string itemName,
            Func<TId, Task<ApiResponse<object>>> deleteFunc,
            string itemType = "elemento")
        {
            if (!DialogHelper.ConfirmDelete(itemName, itemType))
                return false;

            try
            {
                _setLoadingState(true);
                _updateStatus($"Eliminando {itemType}...");

                var result = await deleteFunc(id);

                if (result.Success)
                {
                    _updateStatus($"{itemType} eliminado correctamente");
                    DialogHelper.ShowSuccess($"{itemType} '{itemName}' eliminado correctamente.");
                    _reloadData();
                    return true;
                }
                else
                {
                    _updateStatus($"Error al eliminar {itemType}");
                    DialogHelper.ShowApiError(result, $"eliminar {itemType}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _updateStatus("Error de conexión");
                DialogHelper.ShowConnectionError(ex, $"eliminar {itemType}");
                return false;
            }
            finally
            {
                _setLoadingState(false);
            }
        }

        /// <summary>
        /// Ejecuta una operación de creación con manejo de errores estándar
        /// </summary>
        public async Task<bool> CreateAsync<TData>(
            TData data,
            Func<TData, Task<ApiResponse<object>>> createFunc,
            string itemType = "elemento",
            string successMessage = null)
        {
            try
            {
                _setLoadingState(true);
                _updateStatus($"Creando {itemType}...");

                var result = await createFunc(data);

                if (result.Success)
                {
                    _updateStatus($"{itemType} creado correctamente");
                    DialogHelper.ShowSuccess(successMessage ?? $"{itemType} creado correctamente.");
                    _reloadData();
                    return true;
                }
                else
                {
                    _updateStatus($"Error al crear {itemType}");
                    DialogHelper.ShowApiError(result, $"crear {itemType}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _updateStatus("Error de conexión");
                DialogHelper.ShowConnectionError(ex, $"crear {itemType}");
                return false;
            }
            finally
            {
                _setLoadingState(false);
            }
        }

        /// <summary>
        /// Ejecuta una operación de actualización con manejo de errores estándar
        /// </summary>
        public async Task<bool> UpdateAsync<TData>(
            TData data,
            Func<TData, Task<ApiResponse<object>>> updateFunc,
            string itemType = "elemento",
            string successMessage = null)
        {
            try
            {
                _setLoadingState(true);
                _updateStatus($"Actualizando {itemType}...");

                var result = await updateFunc(data);

                if (result.Success)
                {
                    _updateStatus($"{itemType} actualizado correctamente");
                    DialogHelper.ShowSuccess(successMessage ?? $"{itemType} actualizado correctamente.");
                    _reloadData();
                    return true;
                }
                else
                {
                    _updateStatus($"Error al actualizar {itemType}");
                    DialogHelper.ShowApiError(result, $"actualizar {itemType}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _updateStatus("Error de conexión");
                DialogHelper.ShowConnectionError(ex, $"actualizar {itemType}");
                return false;
            }
            finally
            {
                _setLoadingState(false);
            }
        }

        /// <summary>
        /// Ejecuta una operación de carga de datos con manejo de errores estándar
        /// </summary>
        public async Task<T> LoadAsync<T>(
            Func<Task<ApiResponse<T>>> loadFunc,
            string context,
            bool showSuccessMessage = false)
        {
            try
            {
                _setLoadingState(true);
                _updateStatus($"Cargando {context}...");

                var result = await loadFunc();

                if (result.Success)
                {
                    _updateStatus($"Última actualización: {DateTime.Now:HH:mm:ss}");
                    if (showSuccessMessage)
                    {
                        DialogHelper.ShowSuccess($"{context} cargados correctamente.");
                    }
                    return result.Data;
                }
                else
                {
                    _updateStatus($"Error al cargar {context}");
                    DialogHelper.ShowApiError(result, $"cargar {context}");
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                _updateStatus("Error de conexión");
                DialogHelper.ShowConnectionError(ex, $"cargar {context}");
                return default(T);
            }
            finally
            {
                _setLoadingState(false);
            }
        }
    }

    /// <summary>
    /// Clase helper para gestionar el estado de carga de una ventana
    /// </summary>
    public class LoadingStateManager
    {
        private readonly System.Windows.FrameworkElement _loadingIndicator;
        private readonly System.Collections.Generic.List<System.Windows.FrameworkElement> _controlsToDisable;

        public LoadingStateManager(
            System.Windows.FrameworkElement loadingIndicator,
            params System.Windows.FrameworkElement[] controls)
        {
            _loadingIndicator = loadingIndicator;
            _controlsToDisable = new System.Collections.Generic.List<System.Windows.FrameworkElement>(controls);
        }

        public void SetLoading(bool isLoading)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.Visibility = isLoading
                    ? System.Windows.Visibility.Visible
                    : System.Windows.Visibility.Collapsed;
            }

            foreach (var control in _controlsToDisable)
            {
                control.IsEnabled = !isLoading;
            }
        }
    }

    /// <summary>
    /// Clase helper para gestionar mensajes de estado
    /// </summary>
    public class StatusManager
    {
        private readonly TextBlock _statusTextBlock;

        public StatusManager(TextBlock statusTextBlock)
        {
            _statusTextBlock = statusTextBlock ?? throw new ArgumentNullException(nameof(statusTextBlock));
        }

        public void ShowLoading(string context)
        {
            _statusTextBlock.Text = $"Cargando {context}...";
        }

        public void ShowError(string context)
        {
            _statusTextBlock.Text = $"Error al {context}";
        }

        public void ShowConnectionError()
        {
            _statusTextBlock.Text = "Error de conexión";
        }

        public void ShowSuccess(string context)
        {
            _statusTextBlock.Text = $"{context} completado correctamente";
        }

        public void ShowLastUpdate()
        {
            _statusTextBlock.Text = $"Última actualización: {DateTime.Now:HH:mm:ss}";
        }

        public void ShowCustom(string message)
        {
            _statusTextBlock.Text = message;
        }
    }
}
