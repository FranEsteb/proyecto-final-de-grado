package com.example.android.viewmodel

import android.util.Patterns
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.android.data.RegisterState
import com.example.android.data.local.TokenManager
import com.example.android.data.repository.AuthRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class RegisterViewModel(
    private val authRepository: AuthRepository = AuthRepository(),
    private val tokenManager: TokenManager? = null
) : ViewModel() {

    private val _uiState = MutableStateFlow(RegisterState())
    val uiState: StateFlow<RegisterState> = _uiState.asStateFlow()

    fun onNameChange(name: String) {
        _uiState.update { it.copy(
            name = name,
            isNameError = false,
            nameErrorMessage = null
        ) }
    }

    fun onEmailChange(email: String) {
        _uiState.update { it.copy(
            email = email,
            isEmailError = false,
            emailErrorMessage = null
        ) }
    }

    fun onPasswordChange(password: String) {
        _uiState.update { it.copy(
            password = password,
            isPasswordError = false,
            passwordErrorMessage = null
        ) }
    }

    fun onConfirmPasswordChange(confirmPassword: String) {
        _uiState.update { it.copy(
            confirmPassword = confirmPassword,
            isConfirmPasswordError = false,
            confirmPasswordErrorMessage = null
        ) }
    }

    fun onDniChange(dni: String) {
        _uiState.update { it.copy(
            dni = dni,
            isDniError = false,
            dniErrorMessage = null
        ) }
    }

    fun onCiudadChange(ciudad: String) {
        _uiState.update { it.copy(
            ciudad = ciudad,
            isCiudadError = false,
            ciudadErrorMessage = null
        ) }
    }

    fun onFechaNacimientoChange(fecha: String) {
        _uiState.update { it.copy(
            fechaNacimiento = fecha,
            isFechaNacimientoError = false,
            fechaNacimientoErrorMessage = null
        ) }
    }

    fun togglePasswordVisibility() {
        _uiState.update { it.copy(isPasswordVisible = !it.isPasswordVisible) }
    }

    fun toggleConfirmPasswordVisibility() {
        _uiState.update { it.copy(isConfirmPasswordVisible = !it.isConfirmPasswordVisible) }
    }

    fun onRegisterClick() {
        if (validateInput()) {
            performRegister()
        }
    }

    private fun validateInput(): Boolean {
        val name = _uiState.value.name
        val email = _uiState.value.email
        val password = _uiState.value.password
        val confirmPassword = _uiState.value.confirmPassword
        val dni = _uiState.value.dni
        val ciudad = _uiState.value.ciudad
        val fechaNacimiento = _uiState.value.fechaNacimiento
        var isValid = true

        if (name.isBlank()) {
            _uiState.update { it.copy(
                isNameError = true,
                nameErrorMessage = "El nombre es requerido"
            ) }
            isValid = false
        } else if (name.length < 2) {
            _uiState.update { it.copy(
                isNameError = true,
                nameErrorMessage = "El nombre debe tener al menos 2 caracteres"
            ) }
            isValid = false
        }

        if (dni.isBlank()) {
            _uiState.update { it.copy(
                isDniError = true,
                dniErrorMessage = "El DNI es requerido"
            ) }
            isValid = false
        } else if (!isValidDni(dni)) {
            _uiState.update { it.copy(
                isDniError = true,
                dniErrorMessage = "DNI inválido (ej: 12345678Z)"
            ) }
            isValid = false
        }

        if (email.isBlank()) {
            _uiState.update { it.copy(
                isEmailError = true,
                emailErrorMessage = "El email es requerido"
            ) }
            isValid = false
        } else if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
            _uiState.update { it.copy(
                isEmailError = true,
                emailErrorMessage = "Email inválido"
            ) }
            isValid = false
        }

        if (ciudad.isBlank()) {
            _uiState.update { it.copy(
                isCiudadError = true,
                ciudadErrorMessage = "La ciudad es requerida"
            ) }
            isValid = false
        }

        if (fechaNacimiento.isBlank()) {
            _uiState.update { it.copy(
                isFechaNacimientoError = true,
                fechaNacimientoErrorMessage = "La fecha de nacimiento es requerida"
            ) }
            isValid = false
        } else {
            val datePattern = """^\d{2}/\d{2}/\d{4}$""".toRegex()
            if (!datePattern.matches(fechaNacimiento)) {
                _uiState.update { it.copy(
                    isFechaNacimientoError = true,
                    fechaNacimientoErrorMessage = "Formato inválido. Use DD/MM/YYYY"
                ) }
                isValid = false
            } else {
                val (dateValid, dateError) = validateBirthDate(fechaNacimiento)
                if (!dateValid) {
                    _uiState.update { it.copy(
                        isFechaNacimientoError = true,
                        fechaNacimientoErrorMessage = dateError
                    ) }
                    isValid = false
                }
            }
        }

        if (password.isBlank()) {
            _uiState.update { it.copy(
                isPasswordError = true,
                passwordErrorMessage = "La contraseña es requerida"
            ) }
            isValid = false
        } else if (password.length < 6) {
            _uiState.update { it.copy(
                isPasswordError = true,
                passwordErrorMessage = "La contraseña debe tener al menos 6 caracteres"
            ) }
            isValid = false
        }

        if (confirmPassword.isBlank()) {
            _uiState.update { it.copy(
                isConfirmPasswordError = true,
                confirmPasswordErrorMessage = "Confirma tu contraseña"
            ) }
            isValid = false
        } else if (password != confirmPassword) {
            _uiState.update { it.copy(
                isConfirmPasswordError = true,
                confirmPasswordErrorMessage = "Las contraseñas no coinciden"
            ) }
            isValid = false
        }

        return isValid
    }

    private fun performRegister() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, errorMessage = null) }

            when (val result = authRepository.register(
                _uiState.value.name,
                _uiState.value.email,
                _uiState.value.password,
                _uiState.value.dni,
                _uiState.value.ciudad,
                _uiState.value.fechaNacimiento
            )) {
                is AuthRepository.RegisterResult.Success -> {
                    tokenManager?.saveToken(result.token)

                    _uiState.update { it.copy(
                        isLoading = false,
                        registerSuccess = true
                    ) }
                }
                is AuthRepository.RegisterResult.Error -> {
                    _uiState.update { it.copy(
                        isLoading = false,
                        errorMessage = result.message
                    ) }
                }
            }
        }
    }

    fun clearError() {
        _uiState.update { it.copy(errorMessage = null) }
    }

    fun resetRegisterSuccess() {
        _uiState.update { it.copy(registerSuccess = false) }
    }

    private fun isValidDni(dni: String): Boolean {
        val letras = "TRWAGMYFPDXBNJZSQVHLCKE"
        val pattern = Regex("""^\d{8}[A-Za-z]$""")
        if (!pattern.matches(dni)) return false
        val numero = dni.substring(0, 8).toInt()
        val letraEsperada = letras[numero % 23]
        return dni[8].uppercaseChar() == letraEsperada
    }

    // Valida que la fecha DD/MM/YYYY sea real, no futura y con edad mínima de 16 años
    private fun validateBirthDate(fecha: String): Pair<Boolean, String?> {
        val parts = fecha.split("/")
        val day   = parts[0].toIntOrNull() ?: return Pair(false, "Día inválido")
        val month = parts[1].toIntOrNull() ?: return Pair(false, "Mes inválido")
        val year  = parts[2].toIntOrNull() ?: return Pair(false, "Año inválido")

        val cal = java.util.Calendar.getInstance().apply { isLenient = false }
        try {
            cal.set(year, month - 1, day)
            cal.time // lanza excepción si la fecha no existe (ej: 30/02/2000)
        } catch (e: Exception) {
            return Pair(false, "La fecha no existe (ej: 30/02/2000)")
        }

        val today = java.util.Calendar.getInstance()
        if (cal.after(today)) {
            return Pair(false, "La fecha de nacimiento no puede ser futura")
        }

        val minAge = 16
        val minAgeLimit = java.util.Calendar.getInstance().apply { add(java.util.Calendar.YEAR, -minAge) }
        if (cal.after(minAgeLimit)) {
            return Pair(false, "Debes tener al menos $minAge años")
        }

        return Pair(true, null)
    }
}
