package com.example.android.data.repository

import android.content.Context
import com.example.android.data.ReservaCliente
import com.example.android.data.local.CacheManager
import com.example.android.data.model.CancelarReservaRequest
import com.example.android.data.model.Clase
import com.example.android.data.model.ErrorResponse
import com.example.android.data.model.MiReservaResponse
import com.example.android.data.model.ReservarClaseRequest
import com.example.android.data.remote.RetrofitClient
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

class ReservasRepository(private val context: Context? = null) {

    private val apiService = RetrofitClient.apiService
    private val cacheManager = context?.let { CacheManager(it) }

    sealed class ReservaResult {
        data class Success(val message: String) : ReservaResult()
        data class Error(val message: String) : ReservaResult()
    }

    sealed class ReservasResult {
        data class Success(val reservas: List<ReservaCliente>) : ReservasResult()
        data class Error(val message: String) : ReservasResult()
    }

    suspend fun reservarClase(token: String, idClase: String): ReservaResult {
        return withContext(Dispatchers.IO) {
            try {
                val request = ReservarClaseRequest(idClase)
                val response = apiService.reservarClase("Bearer $token", request)

                if (response.isSuccessful) {
                    // Invalidar caché de reservas Y clases al reservar
                    // (las clases cambian porque se reduce el número de plazas disponibles)
                    cacheManager?.clearReservasCache()
                    cacheManager?.clearClasesCache()

                    response.body()?.let { reservaResponse ->
                        ReservaResult.Success(reservaResponse.message)
                    } ?: ReservaResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al reservar la clase"
                    }
                    ReservaResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                ReservaResult.Error(
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

    suspend fun getMisReservas(token: String): ReservasResult {
        return withContext(Dispatchers.IO) {
            // Intentar obtener del caché primero
            cacheManager?.let { cache ->
                cache.getReservas()?.let { cachedReservas ->
                    val reservasCliente = cachedReservas.mapNotNull { reserva ->
                        reserva.clase?.let { claseEnReserva ->
                            try {
                                val fechaHora = LocalDateTime.parse(
                                    claseEnReserva.fechaHora,
                                    DateTimeFormatter.ISO_DATE_TIME
                                )
                                ReservaCliente(
                                    idReserva = reserva.idReserva,
                                    fecha = fechaHora.toLocalDate(),
                                    clase = Clase.fromClaseEnReserva(claseEnReserva)
                                )
                            } catch (e: Exception) {
                                null
                            }
                        }
                    }
                    return@withContext ReservasResult.Success(reservasCliente)
                }
            }

            // Si no hay caché o expiró, obtener del servidor
            try {
                val response = apiService.getMisReservas("Bearer $token")

                if (response.isSuccessful) {
                    response.body()?.let { misReservas ->
                        // Guardar en caché
                        cacheManager?.saveReservas(misReservas)

                        val reservasCliente = misReservas.mapNotNull { reserva ->
                            reserva.clase?.let { claseEnReserva ->
                                try {
                                    val fechaHora = LocalDateTime.parse(
                                        claseEnReserva.fechaHora,
                                        DateTimeFormatter.ISO_DATE_TIME
                                    )
                                    ReservaCliente(
                                        idReserva = reserva.idReserva,
                                        fecha = fechaHora.toLocalDate(),
                                        clase = Clase.fromClaseEnReserva(claseEnReserva)
                                    )
                                } catch (e: Exception) {
                                    null
                                }
                            }
                        }
                        ReservasResult.Success(reservasCliente)
                    } ?: ReservasResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al obtener las reservas"
                    }
                    ReservasResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                ReservasResult.Error(
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

    suspend fun cancelarReserva(token: String, idReserva: String): ReservaResult {
        return withContext(Dispatchers.IO) {
            try {
                val request = CancelarReservaRequest(idReserva)
                val response = apiService.cancelarReserva("Bearer $token", request)

                if (response.isSuccessful) {
                    // Invalidar caché de reservas Y clases al cancelar
                    // (las clases cambian porque se incrementa el número de plazas disponibles)
                    cacheManager?.clearReservasCache()
                    cacheManager?.clearClasesCache()

                    response.body()?.let { messageResponse ->
                        ReservaResult.Success(messageResponse.message)
                    } ?: ReservaResult.Error("Error en la respuesta del servidor")
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMessage = try {
                        val errorResponse = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        errorResponse.message
                    } catch (e: Exception) {
                        "Error al cancelar la reserva"
                    }
                    ReservaResult.Error(errorMessage)
                }
            } catch (e: Exception) {
                ReservaResult.Error(
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
