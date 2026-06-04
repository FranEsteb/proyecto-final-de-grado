package com.example.android.ui.components

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AccessTime
import androidx.compose.material.icons.filled.CalendarToday
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Place
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import com.example.android.data.model.Clase

@Composable
fun ReservaDialog(
    clase: Clase,
    onDismiss: () -> Unit,
    onConfirm: () -> Unit
) {
    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            elevation = CardDefaults.cardElevation(defaultElevation = 8.dp)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(24.dp)
            ) {
                Text(
                    text = "Reservar Clase",
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.primary
                )

                Spacer(modifier = Modifier.height(16.dp))

                // Nombre de la clase
                Text(
                    text = clase.nombre,
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.SemiBold
                )

                Spacer(modifier = Modifier.height(8.dp))

                // Estado
                EstadoChip(estado = clase.estado)

                Spacer(modifier = Modifier.height(16.dp))

                HorizontalDivider()

                Spacer(modifier = Modifier.height(16.dp))

                // Detalles
                InfoRow(
                    icon = Icons.Default.Person,
                    label = "Instructor",
                    value = clase.instructor
                )

                Spacer(modifier = Modifier.height(12.dp))

                InfoRow(
                    icon = Icons.Default.CalendarToday,
                    label = "Fecha y Hora",
                    value = formatFechaHora(clase.fechaHora)
                )

                Spacer(modifier = Modifier.height(12.dp))

                InfoRow(
                    icon = Icons.Default.AccessTime,
                    label = "Duración",
                    value = "${clase.duracionMinutos} minutos"
                )

                Spacer(modifier = Modifier.height(12.dp))

                InfoRow(
                    icon = Icons.Default.Place,
                    label = "Sala",
                    value = clase.sala
                )

                Spacer(modifier = Modifier.height(16.dp))

                // Descripción
                if (clase.descripcion.isNotEmpty()) {
                    Text(
                        text = "Descripción",
                        style = MaterialTheme.typography.labelLarge,
                        fontWeight = FontWeight.Medium
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    Text(
                        text = clase.descripcion,
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )

                    Spacer(modifier = Modifier.height(16.dp))
                }

                // Disponibilidad
                DisponibilidadIndicator(
                    inscritos = clase.inscritosCount,
                    capacidadMaxima = clase.capacidadMaxima
                )

                Spacer(modifier = Modifier.height(24.dp))

                // Botones
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.End
                ) {
                    TextButton(onClick = onDismiss) {
                        Text("Cancelar")
                    }

                    Spacer(modifier = Modifier.width(8.dp))

                    Button(
                        onClick = onConfirm,
                        enabled = clase.inscritosCount < clase.capacidadMaxima && clase.estado == "disponible"
                    ) {
                        Text(
                            when {
                                clase.estado == "cancelada" -> "Cancelada"
                                clase.inscritosCount >= clase.capacidadMaxima -> "Completo"
                                else -> "Reservar"
                            }
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun InfoRow(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    label: String,
    value: String
) {
    Row(
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            imageVector = icon,
            contentDescription = label,
            modifier = Modifier.size(20.dp),
            tint = MaterialTheme.colorScheme.primary
        )
        Spacer(modifier = Modifier.width(12.dp))
        Column {
            Text(
                text = label,
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                text = value,
                style = MaterialTheme.typography.bodyLarge,
                fontWeight = FontWeight.Medium
            )
        }
    }
}
