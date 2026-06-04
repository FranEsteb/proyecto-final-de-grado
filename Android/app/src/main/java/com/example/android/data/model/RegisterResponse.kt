package com.example.android.data.model

data class RegisterResponse(
    val token: String,
    val user: User? = null
)

data class User(
    val id: String,
    val name: String,
    val email: String
)
