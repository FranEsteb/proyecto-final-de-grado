package com.example.android.data.local

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.longPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import com.example.android.data.model.ClaseResponse
import com.example.android.data.model.MiReservaResponse
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map

class CacheManager(private val context: Context) {

    companion object {
        private val CLASES_KEY = stringPreferencesKey("cached_clases")
        private val CLASES_TIMESTAMP_KEY = longPreferencesKey("clases_timestamp")
        private val RESERVAS_KEY = stringPreferencesKey("cached_reservas")
        private val RESERVAS_TIMESTAMP_KEY = longPreferencesKey("reservas_timestamp")

        // Tiempo de expiración del caché (5 minutos)
        private const val CACHE_EXPIRATION_MS = 5 * 60 * 1000L
    }

    private val gson = Gson()

    // ==================== CLASES ====================

    suspend fun saveClases(clases: List<ClaseResponse>) {
        val json = gson.toJson(clases)
        context.dataStore.edit { preferences ->
            preferences[CLASES_KEY] = json
            preferences[CLASES_TIMESTAMP_KEY] = System.currentTimeMillis()
        }
    }

    suspend fun getClases(): List<ClaseResponse>? {
        val preferences = context.dataStore.data.first()
        val timestamp = preferences[CLASES_TIMESTAMP_KEY] ?: 0L

        // Verificar si el caché ha expirado
        if (System.currentTimeMillis() - timestamp > CACHE_EXPIRATION_MS) {
            return null // Caché expirado
        }

        val json = preferences[CLASES_KEY] ?: return null
        return try {
            val type = object : TypeToken<List<ClaseResponse>>() {}.type
            gson.fromJson(json, type)
        } catch (e: Exception) {
            null
        }
    }

    suspend fun clearClasesCache() {
        context.dataStore.edit { preferences ->
            preferences.remove(CLASES_KEY)
            preferences.remove(CLASES_TIMESTAMP_KEY)
        }
    }

    // ==================== RESERVAS ====================

    suspend fun saveReservas(reservas: List<MiReservaResponse>) {
        val json = gson.toJson(reservas)
        context.dataStore.edit { preferences ->
            preferences[RESERVAS_KEY] = json
            preferences[RESERVAS_TIMESTAMP_KEY] = System.currentTimeMillis()
        }
    }

    suspend fun getReservas(): List<MiReservaResponse>? {
        val preferences = context.dataStore.data.first()
        val timestamp = preferences[RESERVAS_TIMESTAMP_KEY] ?: 0L

        // Verificar si el caché ha expirado
        if (System.currentTimeMillis() - timestamp > CACHE_EXPIRATION_MS) {
            return null // Caché expirado
        }

        val json = preferences[RESERVAS_KEY] ?: return null
        return try {
            val type = object : TypeToken<List<MiReservaResponse>>() {}.type
            gson.fromJson(json, type)
        } catch (e: Exception) {
            null
        }
    }

    suspend fun clearReservasCache() {
        context.dataStore.edit { preferences ->
            preferences.remove(RESERVAS_KEY)
            preferences.remove(RESERVAS_TIMESTAMP_KEY)
        }
    }

    // ==================== LIMPIAR TODO ====================

    suspend fun clearAllCache() {
        clearClasesCache()
        clearReservasCache()
    }
}
