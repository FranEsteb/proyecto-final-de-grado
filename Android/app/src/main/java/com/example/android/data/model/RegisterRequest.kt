package com.example.android.data.model

data class RegisterRequest(
    val name: String,
    val email: String,
    val password: String,
    val dni: String,
    val ciudad: String,
    val fechaNacimiento: String
)
