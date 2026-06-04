package com.example.android.data.local

import android.content.Context
import android.util.Base64
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import com.google.gson.Gson
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "auth_preferences")

class TokenManager(private val context: Context) {

    companion object {
        private val TOKEN_KEY = stringPreferencesKey("auth_token")
        private val USER_ROL_KEY = stringPreferencesKey("user_rol")
        private val USER_ID_KEY = stringPreferencesKey("user_id")
        private val USER_EMAIL_KEY = stringPreferencesKey("user_email")
        private val USER_DNI_KEY = stringPreferencesKey("user_dni")
    }

    val token: Flow<String?> = context.dataStore.data.map { preferences ->
        preferences[TOKEN_KEY]
    }

    val userRol: Flow<String?> = context.dataStore.data.map { preferences ->
        preferences[USER_ROL_KEY]
    }

    val userId: Flow<String?> = context.dataStore.data.map { preferences ->
        preferences[USER_ID_KEY]
    }

    val userEmail: Flow<String?> = context.dataStore.data.map { preferences ->
        preferences[USER_EMAIL_KEY]
    }

    val userDni: Flow<String?> = context.dataStore.data.map { preferences ->
        preferences[USER_DNI_KEY]
    }

    suspend fun saveToken(token: String) {
        context.dataStore.edit { preferences ->
            preferences[TOKEN_KEY] = token
        }

        // Decodificar JWT y guardar información del usuario
        try {
            val payload = decodeJWT(token)
            context.dataStore.edit { preferences ->
                preferences[USER_ROL_KEY] = payload.rol
                preferences[USER_ID_KEY] = payload.id
                preferences[USER_EMAIL_KEY] = payload.email
                payload.dni?.let { preferences[USER_DNI_KEY] = it }
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    suspend fun clearToken() {
        context.dataStore.edit { preferences ->
            preferences.remove(TOKEN_KEY)
            preferences.remove(USER_ROL_KEY)
            preferences.remove(USER_ID_KEY)
            preferences.remove(USER_EMAIL_KEY)
            preferences.remove(USER_DNI_KEY)
        }
    }

    private fun decodeJWT(token: String): JWTPayload {
        val parts = token.split(".")
        if (parts.size != 3) throw IllegalArgumentException("Invalid JWT token")

        val payload = parts[1]
        val decodedBytes = Base64.decode(payload, Base64.URL_SAFE or Base64.NO_WRAP)
        val decodedString = String(decodedBytes, Charsets.UTF_8)

        return Gson().fromJson(decodedString, JWTPayload::class.java)
    }
}

data class JWTPayload(
    val id: String,
    val email: String,
    val rol: String,
    val dni: String?,
    val iat: Long?,
    val exp: Long?
)
