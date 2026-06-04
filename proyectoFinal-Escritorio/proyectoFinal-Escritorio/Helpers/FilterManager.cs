using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace proyectoFinal_Escritorio.Helpers
{
    /// <summary>
    /// Clase helper genérica para gestionar filtrado de colecciones.
    /// Elimina código duplicado de filtrado en todas las ventanas.
    /// </summary>
    public class FilterManager<T>
    {
        private readonly IEnumerable<T> _allItems;
        private readonly ObservableCollection<T> _filteredItems;
        private readonly List<Func<T, bool>> _activeFilters = new();
        private readonly Action<int, int> _updateCountCallback;

        /// <summary>
        /// Constructor del FilterManager
        /// </summary>
        /// <param name="allItems">Colección completa de elementos</param>
        /// <param name="filteredItems">Colección observable para elementos filtrados</param>
        /// <param name="updateCountCallback">Callback opcional para actualizar contadores (filtrados, total)</param>
        public FilterManager(
            IEnumerable<T> allItems,
            ObservableCollection<T> filteredItems,
            Action<int, int> updateCountCallback = null)
        {
            _allItems = allItems ?? throw new ArgumentNullException(nameof(allItems));
            _filteredItems = filteredItems ?? throw new ArgumentNullException(nameof(filteredItems));
            _updateCountCallback = updateCountCallback;
        }

        /// <summary>
        /// Agrega un filtro a la lista de filtros activos
        /// </summary>
        public void AddFilter(Func<T, bool> filter)
        {
            if (filter != null)
            {
                _activeFilters.Add(filter);
            }
        }

        /// <summary>
        /// Limpia todos los filtros activos
        /// </summary>
        public void ClearFilters()
        {
            _activeFilters.Clear();
        }

        /// <summary>
        /// Aplica todos los filtros activos y actualiza la colección filtrada
        /// </summary>
        public void ApplyFilters(Func<IEnumerable<T>, IEnumerable<T>> orderBy = null)
        {
            var filtered = _allItems.AsEnumerable();

            // Aplicar todos los filtros activos
            foreach (var filter in _activeFilters)
            {
                filtered = filtered.Where(filter);
            }

            // Aplicar ordenamiento si se especificó
            if (orderBy != null)
            {
                filtered = orderBy(filtered);
            }

            // Actualizar colección observable
            _filteredItems.Clear();
            foreach (var item in filtered)
            {
                _filteredItems.Add(item);
            }

            // Actualizar contador si se proporcionó callback
            _updateCountCallback?.Invoke(_filteredItems.Count, _allItems.Count());
        }

        /// <summary>
        /// Crea un filtro basado en texto de búsqueda
        /// </summary>
        public static Func<T, bool> CreateTextFilter(
            string searchText,
            params Func<T, string>[] propertySelectors)
        {
            if (string.IsNullOrWhiteSpace(searchText) || propertySelectors == null || propertySelectors.Length == 0)
            {
                return _ => true; // Sin filtro
            }

            return item =>
            {
                foreach (var selector in propertySelectors)
                {
                    var value = selector(item);
                    if (!string.IsNullOrEmpty(value) &&
                        value.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            };
        }

        /// <summary>
        /// Crea un filtro basado en un ComboBox con valor por defecto
        /// </summary>
        public static Func<T, bool> CreateComboBoxFilter<TProp>(
            string selectedValue,
            string defaultValue,
            Func<T, TProp> propertySelector)
        {
            if (string.IsNullOrEmpty(selectedValue) || selectedValue == defaultValue)
            {
                return _ => true; // Sin filtro
            }

            return item =>
            {
                var value = propertySelector(item);
                if (value == null) return false;
                return value.ToString().Equals(selectedValue, StringComparison.OrdinalIgnoreCase);
            };
        }

        /// <summary>
        /// Crea un filtro basado en un rango numérico
        /// </summary>
        public static Func<T, bool> CreateRangeFilter<TValue>(
            TValue? min,
            TValue? max,
            Func<T, TValue> propertySelector)
            where TValue : struct, IComparable<TValue>
        {
            return item =>
            {
                var value = propertySelector(item);

                if (min.HasValue && value.CompareTo(min.Value) < 0)
                    return false;

                if (max.HasValue && value.CompareTo(max.Value) > 0)
                    return false;

                return true;
            };
        }

        /// <summary>
        /// Crea un filtro basado en un rango de fechas
        /// </summary>
        public static Func<T, bool> CreateDateRangeFilter(
            DateTime? startDate,
            DateTime? endDate,
            Func<T, DateTime> propertySelector)
        {
            return item =>
            {
                var value = propertySelector(item);

                if (startDate.HasValue && value < startDate.Value)
                    return false;

                if (endDate.HasValue && value > endDate.Value)
                    return false;

                return true;
            };
        }

        /// <summary>
        /// Crea un filtro basado en una condición booleana
        /// </summary>
        public static Func<T, bool> CreateBooleanFilter(
            bool? filterValue,
            Func<T, bool> propertySelector)
        {
            if (!filterValue.HasValue)
            {
                return _ => true; // Sin filtro
            }

            return item => propertySelector(item) == filterValue.Value;
        }

        /// <summary>
        /// Crea un filtro basado en una lista de valores permitidos
        /// </summary>
        public static Func<T, bool> CreateInListFilter<TProp>(
            IEnumerable<TProp> allowedValues,
            Func<T, TProp> propertySelector)
        {
            if (allowedValues == null || !allowedValues.Any())
            {
                return _ => true; // Sin filtro
            }

            var allowedSet = new HashSet<TProp>(allowedValues);
            return item => allowedSet.Contains(propertySelector(item));
        }
    }

    /// <summary>
    /// Extensiones útiles para colecciones
    /// </summary>
    public static class FilterExtensions
    {
        /// <summary>
        /// Actualiza una ObservableCollection con nuevos elementos de manera eficiente
        /// </summary>
        public static void UpdateWith<T>(this ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (newItems == null) throw new ArgumentNullException(nameof(newItems));

            collection.Clear();
            foreach (var item in newItems)
            {
                collection.Add(item);
            }
        }

        /// <summary>
        /// Filtra una colección usando un predicado de forma fluida
        /// </summary>
        public static IEnumerable<T> WhereIf<T>(
            this IEnumerable<T> source,
            bool condition,
            Func<T, bool> predicate)
        {
            return condition ? source.Where(predicate) : source;
        }
    }
}
