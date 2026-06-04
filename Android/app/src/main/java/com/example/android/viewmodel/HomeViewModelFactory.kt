package com.example.android.viewmodel

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.android.data.local.TokenManager

class HomeViewModelFactory(
    private val tokenManager: TokenManager,
    private val context: Context,
    private val onLogoutComplete: () -> Unit = {}
) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(HomeViewModel::class.java)) {
            @Suppress("UNCHECKED_CAST")
            return HomeViewModel(tokenManager, context, onLogoutComplete) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class")
    }
}
