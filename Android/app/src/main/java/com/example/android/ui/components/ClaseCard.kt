package com.example.android.ui.components

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.android.data.UserRole
import com.example.android.data.model.Clase
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import java.util.*

@Composable
fun ClaseCard(
    clase: Clase,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    userRole: UserRole = UserRole.CLIENTE,
    onEdit: (() -> Unit)? = null,
    onDelete: (() -> Unit)? = null
) {
    Card(
        modifier = modifier
            .fillMaxWidth()
            .clickable(onClick = onClick),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp),
        colors = CardDefaults.cardColors(
            containerColor = when (clase.estado) {
                "cancelada" -> MaterialTheme.colorScheme.errorContainer
                "completa" -> MaterialTheme.colorScheme.tertiaryContainer
                else -> MaterialTheme.colorScheme.surfaceVariant
            }
        )
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp)
        ) {
            // Header con nombre y estado
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = clase.nombre,
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.weight(1f)
                )

                EstadoChip(estado = clase.estado)
            }

            Spacer(modifier = Modifier.height(8.dp))

            // Descripción
            if (clase.descripcion.isNotEmpty()) {
                Text(
                    text = clase.descripcion,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(modifier = Modifier.height(12.dp))
            }

            // Instructor
            Row(
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    imageVector = Icons.Default.Person,
                    contentDescription = "Instructor",
                    modifier = Modifier.size(16.dp),
                    tint = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = clase.instructor,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Spacer(modifier = Modifier.height(8.dp))

            // Fecha y Horario
            Row(
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    imageVector = Icons.Default.CalendarToday,
                    contentDescription = "Fecha",
                    modifier = Modifier.size(16.dp),
                    tint = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = formatFechaHora(clase.fechaHora),
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Spacer(modifier = Modifier.height(8.dp))

            // Duración
            Row(
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    imageVector = Icons.Default.AccessTime,
                    contentDescription = "Duración",
                    modifier = Modifier.size(16.dp),
                    tint = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = "${clase.duracionMinutos} minutos",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Spacer(modifier = Modifier.height(8.dp))

            // Ubicación
            Row(
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    imageVector = Icons.Default.Place,
                    contentDescription = "Sala",
                    modifier = Modifier.size(16.dp),
                    tint = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = clase.sala,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Spacer(modifier = Modifier.height(12.dp))

            // Disponibilidad
            DisponibilidadIndicator(
                inscritos = clase.inscritosCount,
                capacidadMaxima = clase.capacidadMaxima
            )

            // Botones de gestión (solo para admin/empleado)
            if (userRole == UserRole.ADMINISTRADOR || userRole == UserRole.EMPLEADO) {
                Spacer(modifier = Modifier.height(12.dp))
                HorizontalDivider()
                Spacer(modifier = Modifier.height(8.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.End,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    // Botón editar (admin y empleado)
                    onEdit?.let {
                        TextButton(onClick = it) {
                            Icon(
                                imageVector = Icons.Default.Edit,
                                contentDescription = "Editar",
                                modifier = Modifier.size(18.dp)
                            )
                            Spacer(modifier = Modifier.width(4.dp))
                            Text("Editar")
                        }
                    }

                    // Botón eliminar (solo admin)
                    if (userRole == UserRole.ADMINISTRADOR) {
                        Spacer(modifier = Modifier.width(8.dp))
                        onDelete?.let {
                            TextButton(
                                onClick = it,
                                colors = ButtonDefaults.textButtonColors(
                                    contentColor = MaterialTheme.colorScheme.error
                                )
                            ) {
                                Icon(
                                    imageVector = Icons.Default.Delete,
                                    contentDescription = "Eliminar",
                                    modifier = Modifier.size(18.dp)
                                )
                                Spacer(modifier = Modifier.width(4.dp))
                                Text("Eliminar")
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun EstadoChip(estado: String) {
    val (texto, color) = when (estado) {
        "cancelada" -> "Cancelada" to MaterialTheme.colorScheme.error
        "completa" -> "Completa" to MaterialTheme.colorScheme.tertiary
        else -> "Disponible" to MaterialTheme.colorScheme.primary
    }

    AssistChip(
        onClick = { },
        label = { Text(texto, style = MaterialTheme.typography.labelSmall) },
        enabled = false,
        colors = AssistChipDefaults.assistChipColors(
            disabledContainerColor = color.copy(alpha = 0.2f),
            disabledLabelColor = color
        )
    )
}

@Composable
fun DisponibilidadIndicator(
    inscritos: Int,
    capacidadMaxima: Int
) {
    val disponibles = capacidadMaxima - inscritos
    val porcentaje = if (capacidadMaxima > 0) inscritos.toFloat() / capacidadMaxima.toFloat() else 0f

    Column {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                text = "Disponibilidad",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                text = "$disponibles/$capacidadMaxima plazas disponibles",
                style = MaterialTheme.typography.labelMedium,
                color = when {
                    disponibles == 0 -> MaterialTheme.colorScheme.error
                    disponibles <= 3 -> MaterialTheme.colorScheme.tertiary
                    else -> MaterialTheme.colorScheme.primary
                },
                fontWeight = FontWeight.Bold
            )
        }

        Spacer(modifier = Modifier.height(4.dp))

        LinearProgressIndicator(
            progress = { porcentaje },
            modifier = Modifier.fillMaxWidth(),
            color = when {
                porcentaje >= 0.9f -> MaterialTheme.colorScheme.error
                porcentaje >= 0.7f -> MaterialTheme.colorScheme.tertiary
                else -> MaterialTheme.colorScheme.primary
            }
        )
    }
}

// Función para formatear la fecha y hora
fun formatFechaHora(fechaHoraISO: String): String {
    return try {
        val dateTime = LocalDateTime.parse(fechaHoraISO, DateTimeFormatter.ISO_DATE_TIME)
        val formatter = DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm", Locale.forLanguageTag("es-ES"))
        dateTime.format(formatter)
    } catch (e: Exception) {
        fechaHoraISO // Si falla el parseo, devolver el string original
    }
}
