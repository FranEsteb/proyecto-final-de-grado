package com.example.android.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.android.data.local.TokenManager
import com.example.android.data.repository.AuthRepository

class LoginViewModelFactory(
    private val tokenManager: TokenManager
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(LoginViewModel::class.java)) {
            @Suppress("UNCHECKED_CAST")
            return LoginViewModel(
                authRepository = AuthRepository(),
                tokenManager = tokenManager
            ) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class")
    }
}
