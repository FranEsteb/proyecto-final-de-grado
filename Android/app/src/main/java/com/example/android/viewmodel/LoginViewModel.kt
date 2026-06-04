package com.example.android.viewmodel

import android.content.Context
import android.util.Patterns
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.android.data.LoginState
import com.example.android.data.local.TokenManager
import com.example.android.data.repository.AuthRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class LoginViewModel(
    private val authRepository: AuthRepository = AuthRepository(),
    private val tokenManager: TokenManager? = null
) : ViewModel() {

    private val _uiState = MutableStateFlow(LoginState())
    val uiState: StateFlow<LoginState> = _uiState.asStateFlow()

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

    fun togglePasswordVisibility() {
        _uiState.update { it.copy(isPasswordVisible = !it.isPasswordVisible) }
    }

    fun onLoginClick() {
        if (validateInput()) {
            performLogin()
        }
    }

    private fun validateInput(): Boolean {
        val email = _uiState.value.email
        val password = _uiState.value.password
        var isValid = true

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

        if (password.isBlank()) {
            _uiState.update { it.copy(
                isPasswordError = true,
                passwordErrorMessage = "La contraseña es requerida"
            ) }
            isValid = false
        }

        return isValid
    }

    private fun performLogin() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, errorMessage = null) }

            when (val result = authRepository.login(_uiState.value.email, _uiState.value.password)) {
                is AuthRepository.LoginResult.Success -> {
                    // Guardar token
                    tokenManager?.saveToken(result.token)

                    // Obtener email del usuario (usaremos el email como nombre por ahora)
                    val userName = _uiState.value.email.substringBefore("@")

                    _uiState.update { it.copy(
                        isLoading = false,
                        loginSuccess = true,
                        userName = userName
                    ) }
                }
                is AuthRepository.LoginResult.Error -> {
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

    fun resetLoginSuccess() {
        _uiState.update { it.copy(loginSuccess = false, userName = "") }
    }
}
