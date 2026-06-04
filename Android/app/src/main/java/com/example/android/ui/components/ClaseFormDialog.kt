package com.example.android.ui.components

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import com.example.android.data.model.Clase
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ClaseFormDialog(
    title: String,
    claseToEdit: Clase? = null,
    onDismiss: () -> Unit,
    onConfirm: (
        idClase: String,
        nombre: String,
        descripcion: String,
        instructor: String,
        fechaHora: String,
        duracionMinutos: Int,
        capacidadMaxima: Int,
        sala: String
    ) -> Unit
) {
    var idClase by remember { mutableStateOf(claseToEdit?.idClase ?: "") }
    var nombre by remember { mutableStateOf(claseToEdit?.nombre ?: "") }
    var descripcion by remember { mutableStateOf(claseToEdit?.descripcion ?: "") }
    var instructor by remember { mutableStateOf(claseToEdit?.instructor ?: "") }
    var sala by remember { mutableStateOf(claseToEdit?.sala ?: "") }

    // Parsear fecha y hora si es edición
    val (initialFecha, initialHora) = remember {
        if (claseToEdit != null) {
            try {
                val dateTime = LocalDateTime.parse(claseToEdit.fechaHora, DateTimeFormatter.ISO_DATE_TIME)
                val fecha = dateTime.format(DateTimeFormatter.ofPattern("dd/MM/yyyy"))
                val hora = dateTime.format(DateTimeFormatter.ofPattern("HH:mm"))
                Pair(fecha, hora)
            } catch (e: Exception) {
                Pair("", "")
            }
        } else {
            Pair("", "")
        }
    }

    var fecha by remember { mutableStateOf(initialFecha) }
    var hora by remember { mutableStateOf(initialHora) }
    var duracionMinutos by remember { mutableStateOf(claseToEdit?.duracionMinutos?.toString() ?: "60") }
    var capacidadMaxima by remember { mutableStateOf(claseToEdit?.capacidadMaxima?.toString() ?: "20") }

    var errorMessage by remember { mutableStateOf<String?>(null) }

    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .fillMaxHeight(0.9f),
            elevation = CardDefaults.cardElevation(defaultElevation = 8.dp)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
            ) {
                // Header
                TopAppBar(
                    title = { Text(title) },
                    navigationIcon = {
                        IconButton(onClick = onDismiss) {
                            Icon(Icons.Default.Close, "Cerrar")
                        }
                    },
                    colors = TopAppBarDefaults.topAppBarColors(
                        containerColor = MaterialTheme.colorScheme.primaryContainer,
                        titleContentColor = MaterialTheme.colorScheme.onPrimaryContainer
                    )
                )

                // Formulario
                Column(
                    modifier = Modifier
                        .weight(1f)
                        .verticalScroll(rememberScrollState())
                        .padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    // ID de Clase (solo en creación)
                    if (claseToEdit == null) {
                        OutlinedTextField(
                            value = idClase,
                            onValueChange = { idClase = it },
                            label = { Text("ID de Clase *") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true,
                            supportingText = { Text("Identificador único de la clase") }
                        )
                    }

                    // Nombre
                    OutlinedTextField(
                        value = nombre,
                        onValueChange = { nombre = it },
                        label = { Text("Nombre de la clase *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )

                    // Descripción
                    OutlinedTextField(
                        value = descripcion,
                        onValueChange = { descripcion = it },
                        label = { Text("Descripción") },
                        modifier = Modifier.fillMaxWidth(),
                        minLines = 3,
                        maxLines = 5
                    )

                    // Instructor
                    OutlinedTextField(
                        value = instructor,
                        onValueChange = { instructor = it },
                        label = { Text("Instructor") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )

                    // Fecha
                    OutlinedTextField(
                        value = fecha,
                        onValueChange = { fecha = it },
                        label = { Text("Fecha *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        placeholder = { Text("dd/MM/yyyy") },
                        supportingText = { Text("Formato: 31/12/2025") }
                    )

                    // Hora
                    OutlinedTextField(
                        value = hora,
                        onValueChange = { hora = it },
                        label = { Text("Hora *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        placeholder = { Text("HH:mm") },
                        supportingText = { Text("Formato: 14:30") }
                    )

                    // Duración
                    OutlinedTextField(
                        value = duracionMinutos,
                        onValueChange = { duracionMinutos = it },
                        label = { Text("Duración (minutos) *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number)
                    )

                    // Capacidad
                    OutlinedTextField(
                        value = capacidadMaxima,
                        onValueChange = { capacidadMaxima = it },
                        label = { Text("Capacidad máxima *") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number)
                    )

                    // Sala
                    OutlinedTextField(
                        value = sala,
                        onValueChange = { sala = it },
                        label = { Text("Sala") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )

                    // Mensaje de error
                    errorMessage?.let { error ->
                        Card(
                            colors = CardDefaults.cardColors(
                                containerColor = MaterialTheme.colorScheme.errorContainer
                            )
                        ) {
                            Text(
                                text = error,
                                color = MaterialTheme.colorScheme.onErrorContainer,
                                modifier = Modifier.padding(12.dp)
                            )
                        }
                    }
                }

                // Botones
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(16.dp),
                    horizontalArrangement = Arrangement.End
                ) {
                    TextButton(onClick = onDismiss) {
                        Text("Cancelar")
                    }
                    Spacer(modifier = Modifier.width(8.dp))
                    Button(
                        onClick = {
                            // Validación
                            when {
                                claseToEdit == null && idClase.isBlank() -> {
                                    errorMessage = "El ID de clase es obligatorio"
                                }
                                nombre.isBlank() -> {
                                    errorMessage = "El nombre es obligatorio"
                                }
                                fecha.isBlank() -> {
                                    errorMessage = "La fecha es obligatoria"
                                }
                                hora.isBlank() -> {
                                    errorMessage = "La hora es obligatoria"
                                }
                                duracionMinutos.toIntOrNull() == null -> {
                                    errorMessage = "Duración inválida"
                                }
                                capacidadMaxima.toIntOrNull() == null -> {
                                    errorMessage = "Capacidad inválida"
                                }
                                else -> {
                                    // Construir fechaHora en formato ISO 8601
                                    val fechaHoraISO = try {
                                        // Parsear la fecha en formato dd/MM/yyyy
                                        val dateParts = fecha.split("/")
                                        if (dateParts.size != 3) {
                                            errorMessage = "Formato de fecha inválido. Use dd/MM/yyyy"
                                            return@Button
                                        }
                                        val day = dateParts[0].padStart(2, '0')
                                        val month = dateParts[1].padStart(2, '0')
                                        val year = dateParts[2]

                                        // Convertir a formato ISO (yyyy-MM-dd)
                                        "${year}-${month}-${day}T${hora}:00"
                                    } catch (e: Exception) {
                                        errorMessage = "Formato de fecha/hora inválido"
                                        return@Button
                                    }

                                    onConfirm(
                                        claseToEdit?.idClase ?: idClase,
                                        nombre,
                                        descripcion,
                                        instructor,
                                        fechaHoraISO,
                                        duracionMinutos.toInt(),
                                        capacidadMaxima.toInt(),
                                        sala
                                    )
                                }
                            }
                        }
                    ) {
                        Text(if (claseToEdit == null) "Crear" else "Guardar")
                    }
                }
            }
        }
    }
}
