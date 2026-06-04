package com.example.android.data.model

import com.google.gson.annotations.SerializedName

// Modelo para la respuesta del backend
data class ClaseResponse(
    @SerializedName("_id")
    val _id: String,
    val idClase: String,
    val nombre: String,
    val descripcion: String?,
    val instructor: String?,
    val fechaHora: String, // ISO 8601 DateTime
    val duracionMinutos: Int,
    val capacidadMaxima: Int,
    val inscritos: List<UsuarioInscrito>?,
    val sala: String?,
    val estado: String, // disponible, completa, cancelada
    val createdAt: String?,
    val updatedAt: String?
)

// Usuario inscrito (viene en el populate del backend)
data class UsuarioInscrito(
    @SerializedName("_id")
    val _id: String,
    val dni: String,
    val nombre: String,
    val apellidos: String,
    val email: String
)

// Request para crear una clase
data class CreateClaseRequest(
    val idClase: String,
    val nombre: String,
    val descripcion: String?,
    val instructor: String?,
    val fechaHora: String, // ISO 8601 DateTime
    val duracionMinutos: Int,
    val capacidadMaxima: Int,
    val sala: String?,
    val estado: String = "disponible"
)

// Request para actualizar una clase
data class UpdateClaseRequest(
    val idClase: String,
    val nombre: String?,
    val descripcion: String?,
    val instructor: String?,
    val fechaHora: String?,
    val duracionMinutos: Int?,
    val capacidadMaxima: Int?,
    val sala: String?,
    val estado: String?,
    val inscritos: List<String>? // Lista de DNIs
)

// Request para obtener una clase por ID
data class GetClaseRequest(
    val idClase: String
)

// Request para eliminar una clase
data class DeleteClaseRequest(
    val idClase: String
)

// Modelo de UI (conversión desde ClaseResponse)
data class Clase(
    val id: String, // _id de MongoDB
    val idClase: String,
    val nombre: String,
    val descripcion: String,
    val instructor: String,
    val fechaHora: String,
    val duracionMinutos: Int,
    val capacidadMaxima: Int,
    val inscritosCount: Int,
    val sala: String,
    val estado: String
) {
    companion object {
        fun fromResponse(response: ClaseResponse): Clase {
            return Clase(
                id = response._id,
                idClase = response.idClase,
                nombre = response.nombre,
                descripcion = response.descripcion ?: "",
                instructor = response.instructor ?: "Sin instructor",
                fechaHora = response.fechaHora,
                duracionMinutos = response.duracionMinutos,
                capacidadMaxima = response.capacidadMaxima,
                inscritosCount = response.inscritos?.size ?: 0,
                sala = response.sala ?: "Sin asignar",
                estado = response.estado
            )
        }

        fun fromClaseEnReserva(claseEnReserva: ClaseEnReserva): Clase {
            return Clase(
                id = claseEnReserva.idClase, // Usar idClase como ID temporal
                idClase = claseEnReserva.idClase,
                nombre = claseEnReserva.nombre,
                descripcion = claseEnReserva.descripcion ?: "",
                instructor = claseEnReserva.instructor ?: "Sin instructor",
                fechaHora = claseEnReserva.fechaHora,
                duracionMinutos = claseEnReserva.duracionMinutos,
                capacidadMaxima = claseEnReserva.capacidadMaxima,
                inscritosCount = claseEnReserva.inscritosCount,
                sala = claseEnReserva.sala ?: "Sin asignar",
                estado = claseEnReserva.estado
            )
        }
    }
}
