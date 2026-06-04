package com.example.android.data.repository

import android.content.Context
import com.example.android.data.local.CacheManager
import com.example.android.data.model.*
import com.example.android.data.remote.MessageResponse
import com.example.android.data.remote.RetrofitClient
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class ClasesRepository(private val context: Context? = null) {

    private val apiService = RetrofitClient.apiService
    private val cacheManager = context?.let { CacheManager(it) }

    sealed class ClasesResult {
        data class Success(val clases: List<Clase>) : ClasesResult()
        data class Error(val message: String) : ClasesResult()
    }

    sealed class ClaseResult {
        data class Success(val clase: Clase) : ClaseResult()
        data class Error(val message: String) : ClaseResult()
    }

    sealed class OperationResult {
        data class Success(val message: String) : OperationResult()
        data class Error(val message: String) : OperationResult()
    }

    suspend fun getAllClases(token: String): ClasesResult {
        return withContext(Dispatchers.IO) {
            // Intentar obtener del caché primero
            cacheManager?.let { cache ->
                cache.getClases()?.let { cachedClases ->
                    val clases = cachedClases.map { Clase.fromResponse(it) }
                    return@withContext ClasesResult.Success(clases)
                }
            }

            // Si no hay caché o expiró, obtener del servidor
            try {
                val response = apiService.getAllClases("Bearer $token")

                if (response.isSuccessful) {
                    response.body()?.let { clasesResponse ->
                        // Guardar en caché
                        cacheManager?.saveClases(clasesResponse)

                        val clases = clasesResponse.map { Clase.fromResponse(it) }
                        ClasesResult.Success(clases)
                    } ?: ClasesResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al obtener las clases"
                    }
                    ClasesResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                ClasesResult.Error(
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

    suspend fun createClase(token: String, request: CreateClaseRequest): ClaseResult {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.createClase("Bearer $token", request)

                if (response.isSuccessful) {
                    // Invalidar caché al crear
                    cacheManager?.clearClasesCache()

                    response.body()?.let { claseResponse ->
                        ClaseResult.Success(Clase.fromResponse(claseResponse))
                    } ?: ClaseResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al crear la clase"
                    }
                    ClaseResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                ClaseResult.Error(
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

    suspend fun updateClase(token: String, request: UpdateClaseRequest): OperationResult {
        return withContext(Dispatchers.IO) {
            try {
                val response = apiService.updateClase("Bearer $token", request)

                if (response.isSuccessful) {
                    // Invalidar caché al actualizar
                    cacheManager?.clearClasesCache()

                    response.body()?.let { messageResponse ->
                        OperationResult.Success(messageResponse.message)
                    } ?: OperationResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al actualizar la clase"
                    }
                    OperationResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                OperationResult.Error(
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

    suspend fun deleteClase(token: String, idClase: String): OperationResult {
        return withContext(Dispatchers.IO) {
            try {
                val request = DeleteClaseRequest(idClase)
                val response = apiService.deleteClase("Bearer $token", request)

                if (response.isSuccessful) {
                    // Invalidar caché al eliminar
                    cacheManager?.clearClasesCache()

                    response.body()?.let { messageResponse ->
                        OperationResult.Success(messageResponse.message)
                    } ?: OperationResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al eliminar la clase"
                    }
                    OperationResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                OperationResult.Error(
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

    suspend fun getClaseById(token: String, idClase: String): ClaseResult {
        return withContext(Dispatchers.IO) {
            try {
                val request = GetClaseRequest(idClase)
                val response = apiService.getClaseById("Bearer $token", request)

                if (response.isSuccessful) {
                    response.body()?.let { claseResponse ->
                        ClaseResult.Success(Clase.fromResponse(claseResponse))
                    } ?: ClaseResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al obtener la clase"
                    }
                    ClaseResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                ClaseResult.Error(
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
