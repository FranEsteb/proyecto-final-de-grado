package com.example.android.data.remote

import com.example.android.data.model.*
import retrofit2.Response
import retrofit2.http.*

interface ApiService {

    // Auth endpoints
    @POST("api/auth/login")
    suspend fun login(@Body loginRequest: LoginRequest): Response<LoginResponse>

    @POST("api/auth/register")
    suspend fun register(@Body registerRequest: RegisterRequest): Response<RegisterResponse>

    // Clases endpoints
    @GET("api/clases/getAll")
    suspend fun getAllClases(@Header("Authorization") token: String): Response<List<ClaseResponse>>

    @POST("api/clases/new")
    suspend fun createClase(
        @Header("Authorization") token: String,
        @Body request: CreateClaseRequest
    ): Response<ClaseResponse>

    @PATCH("api/clases/update")
    suspend fun updateClase(
        @Header("Authorization") token: String,
        @Body request: UpdateClaseRequest
    ): Response<MessageResponse>

    @HTTP(method = "DELETE", path = "api/clases/delete", hasBody = true)
    suspend fun deleteClase(
        @Header("Authorization") token: String,
        @Body request: DeleteClaseRequest
    ): Response<MessageResponse>

    @POST("api/clases/getOne")
    suspend fun getClaseById(
        @Header("Authorization") token: String,
        @Body request: GetClaseRequest
    ): Response<ClaseResponse>

    // Reservas endpoints
    @POST("api/reservas/reservar")
    suspend fun reservarClase(
        @Header("Authorization") token: String,
        @Body request: ReservarClaseRequest
    ): Response<ReservarClaseResponse>

    @GET("api/reservas/mis-reservas")
    suspend fun getMisReservas(
        @Header("Authorization") token: String
    ): Response<List<MiReservaResponse>>

    @POST("api/reservas/cancelar")
    suspend fun cancelarReserva(
        @Header("Authorization") token: String,
        @Body request: CancelarReservaRequest
    ): Response<MessageResponse>
}

// Response genérica para mensajes
data class MessageResponse(
    val message: String
)
