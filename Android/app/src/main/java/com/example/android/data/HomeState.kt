package com.example.android.data

import com.example.android.data.model.Clase
import java.time.LocalDate

enum class HomeSection {
    INICIO,
    CLASES,
    RESERVAS
}

enum class UserRole {
    CLIENTE,
    EMPLEADO,
    ADMINISTRADOR;

    companion object {
        fun fromString(rol: String?): UserRole {
            return when (rol?.lowercase()) {
                "administrador" -> ADMINISTRADOR
                "empleado" -> EMPLEADO
                else -> CLIENTE
            }
        }
    }
}

data class ReservaCliente(
    val idReserva: String,
    val fecha: LocalDate,
    val clase: Clase
)

data class HomeState(
    val userName: String = "",
    val userRole: UserRole = UserRole.CLIENTE,
    val currentSection: HomeSection = HomeSection.INICIO,
    val selectedDate: LocalDate = LocalDate.now(),
    val clases: List<Clase> = emptyList(),
    val filteredClases: List<Clase> = emptyList(),
    val selectedClase: Clase? = null,
    val reservasCliente: List<ReservaCliente> = emptyList(),
    val isLoading: Boolean = false,
    val showReservaDialog: Boolean = false,
    val showCreateClaseDialog: Boolean = false,
    val showEditClaseDialog: Boolean = false,
    val showDeleteConfirmDialog: Boolean = false,
    val showCancelReservaDialog: Boolean = false,
    val showLogoutConfirmDialog: Boolean = false,
    val claseToEdit: Clase? = null,
    val claseToDelete: Clase? = null,
    val reservaToCancel: ReservaCliente? = null,
    val errorMessage: String? = null,
    val successMessage: String? = null
)
