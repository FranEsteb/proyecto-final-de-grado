package com.example.android.data.repository

import com.example.android.data.model.ErrorResponse
import com.example.android.data.model.LoginRequest
import com.example.android.data.model.RegisterRequest
import com.example.android.data.remote.RetrofitClient
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class AuthRepository {

    private val apiService = RetrofitClient.apiService

    sealed class LoginResult {
        data class Success(val token: String) : LoginResult()
        data class Error(val message: String) : LoginResult()
    }

    sealed class RegisterResult {
        data class Success(val token: String) : RegisterResult()
        data class Error(val message: String) : RegisterResult()
    }

    suspend fun login(email: String, password: String): LoginResult {
        return withContext(Dispatchers.IO) {
            try {
                val request = LoginRequest(email, password)
                val response = apiService.login(request)

                if (response.isSuccessful) {
                    response.body()?.let { loginResponse ->
                        LoginResult.Success(loginResponse.token)
                    } ?: LoginResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al iniciar sesión"
                    }
                    LoginResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                LoginResult.Error(
                    when {
                        e.message?.contains("Unable to resolve host") == true ->
                            "No se puede conectar al servidor. Verifica tu conexión."
                        e.message?.contains("timeout") == true ->
                            "Tiempo de espera agotado. Intenta nuevamente."
                        else ->
                            "Error de conexión: ${e.message ?: "Desconocido"}"
                    }
                )
            }
        }
    }

    suspend fun register(name: String, email: String, password: String, dni: String, ciudad: String, fechaNacimiento: String): RegisterResult {
        return withContext(Dispatchers.IO) {
            try {
                val request = RegisterRequest(name, email, password, dni, ciudad, fechaNacimiento)
                val response = apiService.register(request)

                if (response.isSuccessful) {
                    response.body()?.let { registerResponse ->
                        RegisterResult.Success(registerResponse.token)
                    } ?: RegisterResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al registrar usuario"
                    }
                    RegisterResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                RegisterResult.Error(
                    when {
                        e.message?.contains("Unable to resolve host") == true ->
                            "No se puede conectar al servidor. Verifica tu conexión."
                        e.message?.contains("timeout") == true ->
                            "Tiempo de espera agotado. Intenta nuevamente."
                        else ->
                            "Error de conexión: ${e.message ?: "Desconocido"}"
                    }
                )
            }
        }
    }
}
