package com.example.android

import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.android.data.local.TokenManager
import com.example.android.ui.screens.HomeScreen
import com.example.android.ui.screens.LoginScreen
import com.example.android.ui.screens.RegisterScreen
import com.example.android.ui.theme.AndroidTheme
import com.example.android.viewmodel.LoginViewModel
import com.example.android.viewmodel.LoginViewModelFactory
import com.example.android.viewmodel.RegisterViewModel
import com.example.android.viewmodel.RegisterViewModelFactory

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        val tokenManager = TokenManager(this)

        setContent {
            AndroidTheme {
                var currentScreen by remember { mutableStateOf("login") }
                var userName by remember { mutableStateOf("Usuario") }

                when (currentScreen) {
                    "login" -> {
                        val viewModel: LoginViewModel = viewModel(
                            factory = LoginViewModelFactory(tokenManager)
                        )

                        LoginScreen(
                            viewModel = viewModel,
                            onLoginSuccess = { name ->
                                userName = name
                                Toast.makeText(
                                    this,
                                    "Login exitoso!",
                                    Toast.LENGTH_SHORT
                                ).show()
                                currentScreen = "home"
                            },
                            onNavigateToRegister = {
                                currentScreen = "register"
                            }
                        )
                    }
                    "register" -> {
                        val viewModel: RegisterViewModel = viewModel(
                            factory = RegisterViewModelFactory(tokenManager)
                        )

                        RegisterScreen(
                            viewModel = viewModel,
                            onRegisterSuccess = { name ->
                                userName = name
                                Toast.makeText(
                                    this,
                                    "Registro exitoso!",
                                    Toast.LENGTH_SHORT
                                ).show()
                                currentScreen = "home"
                            },
                            onNavigateToLogin = {
                                currentScreen = "login"
                            }
                        )
                    }
                    "home" -> {
                        val homeViewModel: com.example.android.viewmodel.HomeViewModel = viewModel(
                            factory = com.example.android.viewmodel.HomeViewModelFactory(
                                tokenManager = tokenManager,
                                context = this,
                                onLogoutComplete = {
                                    Toast.makeText(
                                        this,
                                        "Sesión cerrada",
                                        Toast.LENGTH_SHORT
                                    ).show()
                                    currentScreen = "login"
                                }
                            )
                        )

                        HomeScreen(
                            viewModel = homeViewModel,
                            userName = userName
                        )
                    }
                }
            }
        }
    }
}