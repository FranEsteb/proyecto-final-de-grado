package com.example.android.data.model

// Request para reservar una clase
data class ReservarClaseRequest(
    val idClase: String
)

// Request para cancelar una reserva
data class CancelarReservaRequest(
    val idReserva: String
)

// Response del servidor al crear una reserva
data class ReservarClaseResponse(
    val message: String,
    val reserva: ReservaResponse?
)

data class ReservaResponse(
    val idReserva: String,
    val usuario: String,
    val clase: String,
    val fechaReserva: String,
    val estado: String,
    val observaciones: String?
)

// Response de "mis reservas"
data class MiReservaResponse(
    val idReserva: String,
    val fechaReserva: String,
    val estado: String,
    val clase: ClaseEnReserva?
)

// Clase simplificada que viene en las reservas (con inscritosCount en lugar de inscritos)
data class ClaseEnReserva(
    val idClase: String,
    val nombre: String,
    val descripcion: String?,
    val instructor: String?,
    val fechaHora: String,
    val duracionMinutos: Int,
    val capacidadMaxima: Int,
    val inscritosCount: Int,
    val sala: String?,
    val estado: String
)
