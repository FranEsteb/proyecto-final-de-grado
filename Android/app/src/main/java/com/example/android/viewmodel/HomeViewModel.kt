package com.example.android.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.android.data.HomeSection
import com.example.android.data.HomeState
import com.example.android.data.UserRole
import com.example.android.data.local.TokenManager
import com.example.android.data.model.Clase
import com.example.android.data.repository.ClasesRepository
import com.example.android.data.repository.ReservasRepository
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch

class HomeViewModel(
    private val tokenManager: TokenManager,
    private val context: android.content.Context,
    private val onLogoutComplete: () -> Unit = {}
) : ViewModel() {

    private val clasesRepository = ClasesRepository(context)
    private val reservasRepository = ReservasRepository(context)

    private val _uiState = MutableStateFlow(HomeState())
    val uiState: StateFlow<HomeState> = _uiState.asStateFlow()

    init {
        loadUserRole()
        loadClases()
        loadReservas()
    }

    private fun loadUserRole() {
        viewModelScope.launch {
            tokenManager.userRol.collect { rol ->
                _uiState.update {
                    it.copy(userRole = UserRole.fromString(rol))
                }
            }
        }
    }

    private fun loadClases() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true) }

            tokenManager.token.firstOrNull()?.let { token ->
                when (val result = clasesRepository.getAllClases(token)) {
                    is ClasesRepository.ClasesResult.Success -> {
                        _uiState.update {
                            it.copy(
                                clases = result.clases,
                                isLoading = false,
                                errorMessage = null
                            )
                        }
                    }
                    is ClasesRepository.ClasesResult.Error -> {
                        _uiState.update {
                            it.copy(
                                isLoading = false,
                                errorMessage = result.message
                            )
                        }
                    }
                }
            } ?: run {
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        errorMessage = "No se encontró el token de autenticación"
                    )
                }
            }
        }
    }

    fun navigateToSection(section: HomeSection) {
        _uiState.update {
            it.copy(currentSection = section)
        }
        // Recargar reservas cuando se navega a la sección de reservas
        if (section == HomeSection.RESERVAS) {
            loadReservas()
        }
    }

    private fun loadReservas() {
        viewModelScope.launch {
            println("DEBUG: Iniciando carga de reservas")
            _uiState.update { it.copy(isLoading = true) }

            tokenManager.token.firstOrNull()?.let { token ->
                println("DEBUG: Token obtenido, llamando a getMisReservas")
                when (val result = reservasRepository.getMisReservas(token)) {
                    is ReservasRepository.ReservasResult.Success -> {
                        println("DEBUG: Reservas obtenidas exitosamente: ${result.reservas.size} reservas")
                        result.reservas.forEach { reserva ->
                            println("DEBUG: Reserva - Fecha: ${reserva.fecha}, Clase: ${reserva.clase.nombre}")
                        }
                        _uiState.update {
                            it.copy(
                                reservasCliente = result.reservas,
                                isLoading = false,
                                errorMessage = null
                            )
                        }
                    }
                    is ReservasRepository.ReservasResult.Error -> {
                        println("DEBUG: Error al obtener reservas: ${result.message}")
                        _uiState.update {
                            it.copy(
                                isLoading = false,
                                errorMessage = result.message
                            )
                        }
                    }
                }
            } ?: run {
                println("DEBUG: No se encontró token")
                _uiState.update {
                    it.copy(
                        isLoading = false,
                        errorMessage = "No se encontró el token de autenticación"
                    )
                }
            }
        }
    }

    fun onClaseClick(clase: Clase) {
        _uiState.update {
            it.copy(
                selectedClase = clase,
                showReservaDialog = true
            )
        }
    }

    fun onDismissReservaDialog() {
        _uiState.update {
            it.copy(
                selectedClase = null,
                showReservaDialog = false
            )
        }
    }

    fun onReservarClase() {
        viewModelScope.launch {
            _uiState.value.selectedClase?.let { clase ->
                _uiState.update { it.copy(isLoading = true) }

                tokenManager.token.firstOrNull()?.let { token ->
                    when (val result = reservasRepository.reservarClase(token, clase.idClase)) {
                        is ReservasRepository.ReservaResult.Success -> {
                            _uiState.update {
                                it.copy(
                                    isLoading = false,
                                    selectedClase = null,
                                    showReservaDialog = false,
                                    successMessage = result.message,
                                    errorMessage = null
                                )
                            }
                            // Recargar las clases para actualizar los inscritos
                            loadClases()
                            // Recargar reservas para que aparezca en el calendario
                            loadReservas()
                        }
                        is ReservasRepository.ReservaResult.Error -> {
                            _uiState.update {
                                it.copy(
                                    isLoading = false,
                                    selectedClase = null,
                                    showReservaDialog = false,
                                    errorMessage = result.message
                                )
                            }
                        }
                    }
                } ?: run {
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            selectedClase = null,
                            showReservaDialog = false,
                            errorMessage = "No se encontró el token de autenticación"
                        )
                    }
                }
            }
        }
    }

    fun setUserName(name: String) {
        _uiState.update { it.copy(userName = name) }
    }

    fun refreshClases() {
        loadClases()
    }

    // Funciones para gestión de clases
    fun onCreateClase() {
        _uiState.update { it.copy(showCreateClaseDialog = true) }
    }

    fun onEditClase(clase: Clase) {
        _uiState.update {
            it.copy(
                claseToEdit = clase,
                showEditClaseDialog = true
            )
        }
    }

    fun onDeleteClase(clase: Clase) {
        _uiState.update {
            it.copy(
                claseToDelete = clase,
                showDeleteConfirmDialog = true
            )
        }
    }

    fun onDismissCreateClaseDialog() {
        _uiState.update { it.copy(showCreateClaseDialog = false) }
    }

    fun onDismissEditClaseDialog() {
        _uiState.update {
            it.copy(
                showEditClaseDialog = false,
                claseToEdit = null
            )
        }
    }

    fun onDismissDeleteConfirmDialog() {
        _uiState.update {
            it.copy(
                showDeleteConfirmDialog = false,
                claseToDelete = null
            )
        }
    }

    fun createClase(
        idClase: String,
        nombre: String,
        descripcion: String,
        instructor: String,
        fechaHora: String,
        duracionMinutos: Int,
        capacidadMaxima: Int,
        sala: String
    ) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true) }

            tokenManager.token.firstOrNull()?.let { token ->
                val request = com.example.android.data.model.CreateClaseRequest(
                    idClase = idClase,
                    nombre = nombre,
                    descripcion = descripcion,
                    instructor = instructor,
                    fechaHora = fechaHora,
                    duracionMinutos = duracionMinutos,
                    capacidadMaxima = capacidadMaxima,
                    sala = sala,
                    estado = "disponible"
                )

                when (val result = clasesRepository.createClase(token, request)) {
                    is ClasesRepository.ClaseResult.Success -> {
                        _uiState.update {
                            it.copy(
                                isLoading = false,
                                showCreateClaseDialog = false,
                                successMessage = "Clase creada exitosamente",
                                errorMessage = null
                            )
                        }
                        loadClases()
                    }
                    is ClasesRepository.ClaseResult.Error -> {
                        _uiState.update {
                            it.copy(
                                isLoading = false,
                                errorMessage = result.message
                            )
                        }
                    }
                }
            }
        }
    }

    fun updateClase(
        idClase: String,
        nombre: String?,
        descripcion: String?,
        instructor: String?,
        fechaHora: String?,
        duracionMinutos: Int?,
        capacidadMaxima: Int?,
        sala: String?
    ) {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true) }

            tokenManager.token.firstOrNull()?.let { token ->
                val request = com.example.android.data.model.UpdateClaseRequest(
                    idClase = idClase,
                    nombre = nombre,
                    descripcion = descripcion,
                    instructor = instructor,
                    fechaHora = fechaHora,
                    duracionMinutos = duracionMinutos,
                    capacidadMaxima = capacidadMaxima,
                    sala = sala,
                    estado = null,
                    inscritos = null
                )

                when (val result = clasesRepository.updateClase(token, request)) {
                    is ClasesRepository.OperationResult.Success -> {
                        _uiState.update {
                            it.copy(
                                isLoading = false,
                                showEditClaseDialog = false,
                                claseToEdit = null,
                                successMessage = "Clase actualizada exitosamente",
                                errorMessage = null
                            )
                        }
                        loadClases()
                    }
                    is ClasesRepository.OperationResult.Error -> {
                        _uiState.update {
                            it.copy(
                                isLoading = false,
                                errorMessage = result.message
                            )
                        }
                    }
                }
            }
        }
    }

    fun confirmDeleteClase() {
        viewModelScope.launch {
            _uiState.value.claseToDelete?.let { clase ->
                _uiState.update { it.copy(isLoading = true) }

                tokenManager.token.firstOrNull()?.let { token ->
                    when (val result = clasesRepository.deleteClase(token, clase.idClase)) {
                        is ClasesRepository.OperationResult.Success -> {
                            _uiState.update {
                                it.copy(
                                    isLoading = false,
                                    showDeleteConfirmDialog = false,
                                    claseToDelete = null,
                                    successMessage = "Clase eliminada exitosamente",
                                    errorMessage = null
                                )
                            }
                            loadClases()
                        }
                        is ClasesRepository.OperationResult.Error -> {
                            _uiState.update {
                                it.copy(
                                    isLoading = false,
                                    showDeleteConfirmDialog = false,
                                    claseToDelete = null,
                                    errorMessage = result.message
                                )
                            }
                        }
                    }
                }
            }
        }
    }

    fun clearMessages() {
        _uiState.update {
            it.copy(
                successMessage = null,
                errorMessage = null
            )
        }
    }

    // Funciones para logout
    fun onLogoutClick() {
        _uiState.update {
            it.copy(showLogoutConfirmDialog = true)
        }
    }

    fun onDismissLogoutDialog() {
        _uiState.update {
            it.copy(showLogoutConfirmDialog = false)
        }
    }

    fun confirmLogout() {
        viewModelScope.launch {
            tokenManager.clearToken()
            _uiState.update {
                it.copy(showLogoutConfirmDialog = false)
            }
            onLogoutComplete()
        }
    }

    // Funciones para cancelar reservas
    fun onCancelReserva(reserva: com.example.android.data.ReservaCliente) {
        _uiState.update {
            it.copy(
                reservaToCancel = reserva,
                showCancelReservaDialog = true
            )
        }
    }

    fun onDismissCancelReservaDialog() {
        _uiState.update {
            it.copy(
                showCancelReservaDialog = false,
                reservaToCancel = null
            )
        }
    }

    fun confirmCancelReserva() {
        viewModelScope.launch {
            _uiState.value.reservaToCancel?.let { reserva ->
                _uiState.update { it.copy(isLoading = true) }

                tokenManager.token.firstOrNull()?.let { token ->
                    when (val result = reservasRepository.cancelarReserva(token, reserva.idReserva)) {
                        is ReservasRepository.ReservaResult.Success -> {
                            _uiState.update {
                                it.copy(
                                    isLoading = false,
                                    showCancelReservaDialog = false,
                                    reservaToCancel = null,
                                    successMessage = result.message,
                                    errorMessage = null
                                )
                            }
                            // Recargar las clases para actualizar los inscritos
                            loadClases()
                            // Recargar reservas para actualizar la lista
                            loadReservas()
                        }
                        is ReservasRepository.ReservaResult.Error -> {
                            _uiState.update {
                                it.copy(
                                    isLoading = false,
                                    showCancelReservaDialog = false,
                                    reservaToCancel = null,
                                    errorMessage = result.message
                                )
                            }
                        }
                    }
                } ?: run {
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            showCancelReservaDialog = false,
                            reservaToCancel = null,
                            errorMessage = "No se encontró el token de autenticación"
                        )
                    }
                }
            }
        }
    }
}
