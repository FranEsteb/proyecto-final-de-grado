package com.example.android.ui.screens

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.android.data.HomeSection
import com.example.android.data.ReservaCliente
import com.example.android.ui.components.ClaseCard
import com.example.android.ui.components.ClaseFormDialog
import com.example.android.ui.components.DeleteConfirmDialog
import com.example.android.ui.components.ReservaDialog
import com.example.android.ui.components.formatFechaHora
import com.example.android.viewmodel.HomeViewModel
import java.time.LocalDate
import java.time.YearMonth
import java.time.format.TextStyle
import java.util.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    viewModel: HomeViewModel = viewModel(),
    userName: String = "Usuario"
) {
    val uiState by viewModel.uiState.collectAsState()

    LaunchedEffect(userName) {
        viewModel.setUserName(userName)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            text = "FitGym",
                            style = MaterialTheme.typography.headlineSmall,
                            fontWeight = FontWeight.Bold
                        )
                        Text(
                            text = "Bienvenido, $userName",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                },
                navigationIcon = {
                    if (uiState.currentSection != HomeSection.INICIO) {
                        IconButton(onClick = { viewModel.navigateToSection(HomeSection.INICIO) }) {
                            Icon(
                                imageVector = Icons.Default.ArrowBack,
                                contentDescription = "Volver"
                            )
                        }
                    }
                },
                actions = {
                    IconButton(onClick = { viewModel.onLogoutClick() }) {
                        Icon(
                            imageVector = Icons.Default.Logout,
                            contentDescription = "Cerrar sesión",
                            tint = MaterialTheme.colorScheme.onPrimaryContainer
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primaryContainer,
                    titleContentColor = MaterialTheme.colorScheme.onPrimaryContainer
                )
            )
        }
    ) { paddingValues ->
        when (uiState.currentSection) {
            HomeSection.INICIO -> InicioContent(
                modifier = Modifier.padding(paddingValues),
                onClasesClick = { viewModel.navigateToSection(HomeSection.CLASES) },
                onReservasClick = { viewModel.navigateToSection(HomeSection.RESERVAS) }
            )
            HomeSection.CLASES -> ClasesContent(
                modifier = Modifier.padding(paddingValues),
                clases = uiState.clases,
                isLoading = uiState.isLoading,
                errorMessage = uiState.errorMessage,
                userRole = uiState.userRole,
                onClaseClick = { viewModel.onClaseClick(it) },
                onEditClase = { viewModel.onEditClase(it) },
                onDeleteClase = { viewModel.onDeleteClase(it) },
                onCreateClase = { viewModel.onCreateClase() }
            )
            HomeSection.RESERVAS -> ReservasContent(
                modifier = Modifier.padding(paddingValues),
                reservas = uiState.reservasCliente,
                onCancelReserva = { viewModel.onCancelReserva(it) }
            )
        }
    }

    // Diálogo de reserva
    if (uiState.showReservaDialog && uiState.selectedClase != null) {
        ReservaDialog(
            clase = uiState.selectedClase!!,
            onDismiss = { viewModel.onDismissReservaDialog() },
            onConfirm = { viewModel.onReservarClase() }
        )
    }

    // Diálogo para crear clase
    if (uiState.showCreateClaseDialog) {
        ClaseFormDialog(
            title = "Crear Nueva Clase",
            onDismiss = { viewModel.onDismissCreateClaseDialog() },
            onConfirm = { idClase, nombre, descripcion, instructor, fechaHora, duracionMinutos, capacidadMaxima, sala ->
                viewModel.createClase(
                    idClase = idClase,
                    nombre = nombre,
                    descripcion = descripcion,
                    instructor = instructor,
                    fechaHora = fechaHora,
                    duracionMinutos = duracionMinutos,
                    capacidadMaxima = capacidadMaxima,
                    sala = sala
                )
            }
        )
    }

    // Diálogo para editar clase
    if (uiState.showEditClaseDialog && uiState.claseToEdit != null) {
        ClaseFormDialog(
            title = "Editar Clase",
            claseToEdit = uiState.claseToEdit,
            onDismiss = { viewModel.onDismissEditClaseDialog() },
            onConfirm = { idClase, nombre, descripcion, instructor, fechaHora, duracionMinutos, capacidadMaxima, sala ->
                viewModel.updateClase(
                    idClase = idClase,
                    nombre = nombre,
                    descripcion = descripcion,
                    instructor = instructor,
                    fechaHora = fechaHora,
                    duracionMinutos = duracionMinutos,
                    capacidadMaxima = capacidadMaxima,
                    sala = sala
                )
            }
        )
    }

    // Diálogo de confirmación para eliminar
    if (uiState.showDeleteConfirmDialog && uiState.claseToDelete != null) {
        DeleteConfirmDialog(
            title = "Eliminar Clase",
            message = "¿Estás seguro de que deseas eliminar la clase \"${uiState.claseToDelete!!.nombre}\"? Esta acción no se puede deshacer.",
            onDismiss = { viewModel.onDismissDeleteConfirmDialog() },
            onConfirm = { viewModel.confirmDeleteClase() }
        )
    }

    // Diálogo de confirmación para cancelar reserva
    if (uiState.showCancelReservaDialog && uiState.reservaToCancel != null) {
        DeleteConfirmDialog(
            title = "Cancelar Reserva",
            message = "¿Estás seguro de que deseas cancelar tu reserva para la clase \"${uiState.reservaToCancel!!.clase.nombre}\"?",
            onDismiss = { viewModel.onDismissCancelReservaDialog() },
            onConfirm = { viewModel.confirmCancelReserva() }
        )
    }

    // Diálogo de confirmación para cerrar sesión
    if (uiState.showLogoutConfirmDialog) {
        DeleteConfirmDialog(
            title = "Cerrar Sesión",
            message = "¿Estás seguro de que deseas cerrar sesión?",
            onDismiss = { viewModel.onDismissLogoutDialog() },
            onConfirm = { viewModel.confirmLogout() },
            confirmText = "Aceptar"
        )
    }

    // Mostrar mensajes de éxito/error
    uiState.successMessage?.let { message ->
        LaunchedEffect(message) {
            kotlinx.coroutines.delay(3000)
            viewModel.clearMessages()
        }
    }
}

@Composable
fun InicioContent(
    modifier: Modifier = Modifier,
    onClasesClick: () -> Unit,
    onReservasClick: () -> Unit
) {
    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item {
            Spacer(modifier = Modifier.height(8.dp))
        }

        item {
            Text(
                text = "¿Qué deseas hacer?",
                style = MaterialTheme.typography.headlineMedium,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.primary
            )
        }

        item {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                MenuCard(
                    title = "Clases",
                    icon = Icons.Default.FitnessCenter,
                    description = "Ver todas las clases disponibles",
                    onClick = onClasesClick,
                    modifier = Modifier.weight(1f)
                )

                MenuCard(
                    title = "Reservas",
                    icon = Icons.Default.CalendarMonth,
                    description = "Ver tus clases reservadas",
                    onClick = onReservasClick,
                    modifier = Modifier.weight(1f)
                )
            }
        }

        item {
            Spacer(modifier = Modifier.height(8.dp))
        }

        item {
            GymInfoCard()
        }

        item {
            Spacer(modifier = Modifier.height(16.dp))
        }
    }
}

@Composable
fun MenuCard(
    title: String,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    description: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier
            .aspectRatio(1f)
            .clickable(onClick = onClick),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.primaryContainer
        )
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(20.dp),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Icon(
                imageVector = icon,
                contentDescription = title,
                modifier = Modifier.size(64.dp),
                tint = MaterialTheme.colorScheme.primary
            )

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                text = title,
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.onPrimaryContainer
            )

            Spacer(modifier = Modifier.height(8.dp))

            Text(
                text = description,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onPrimaryContainer,
                textAlign = TextAlign.Center
            )
        }
    }
}

@Composable
fun GymInfoCard() {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.secondaryContainer
        ),
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(20.dp)
        ) {
            Row(
                verticalAlignment = Alignment.CenterVertically
            ) {
                Icon(
                    imageVector = Icons.Default.FitnessCenter,
                    contentDescription = "Gym",
                    modifier = Modifier.size(40.dp),
                    tint = MaterialTheme.colorScheme.primary
                )
                Spacer(modifier = Modifier.width(12.dp))
                Column {
                    Text(
                        text = "FitGym Premium",
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onSecondaryContainer
                    )
                    Text(
                        text = "Centro de Alto Rendimiento",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSecondaryContainer.copy(alpha = 0.8f)
                    )
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                text = "Tu centro de fitness completo con las mejores instalaciones, equipamiento de última generación y profesionales certificados. Ofrecemos más de 50 clases semanales en diferentes disciplinas.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSecondaryContainer,
                lineHeight = MaterialTheme.typography.bodyMedium.lineHeight.times(1.3f)
            )

            Spacer(modifier = Modifier.height(20.dp))

            HorizontalDivider(color = MaterialTheme.colorScheme.onSecondaryContainer.copy(alpha = 0.2f))

            Spacer(modifier = Modifier.height(16.dp))

            // Ubicación
            GymInfoRow(
                icon = Icons.Default.Place,
                title = "Dirección",
                content = "Calle Colón, 45, 46004 Valencia"
            )

            Spacer(modifier = Modifier.height(12.dp))

            // Horario
            GymInfoRow(
                icon = Icons.Default.Schedule,
                title = "Horario",
                content = "Lunes a Viernes: 7:00 - 23:00\nSábados y Domingos: 9:00 - 21:00"
            )

            Spacer(modifier = Modifier.height(12.dp))

            // Teléfono
            GymInfoRow(
                icon = Icons.Default.Phone,
                title = "Contacto",
                content = "+34 963 456 789"
            )

            Spacer(modifier = Modifier.height(12.dp))

            // Email
            GymInfoRow(
                icon = Icons.Default.Email,
                title = "Email",
                content = "info@fitgympremium.com"
            )

            Spacer(modifier = Modifier.height(20.dp))

            HorizontalDivider(color = MaterialTheme.colorScheme.onSecondaryContainer.copy(alpha = 0.2f))

            Spacer(modifier = Modifier.height(16.dp))

            // Membresía
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column {
                    Row(
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Icon(
                            imageVector = Icons.Default.CardMembership,
                            contentDescription = "Membresía",
                            modifier = Modifier.size(24.dp),
                            tint = MaterialTheme.colorScheme.primary
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = "Membresía Activa",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.onSecondaryContainer
                        )
                    }
                    Spacer(modifier = Modifier.height(4.dp))
                    Text(
                        text = "Plan Premium Mensual",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSecondaryContainer.copy(alpha = 0.7f)
                    )
                }

                Column(
                    horizontalAlignment = Alignment.End
                ) {
                    Text(
                        text = "49,99€",
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary
                    )
                    Text(
                        text = "/mes",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSecondaryContainer.copy(alpha = 0.7f)
                    )
                }
            }

            Spacer(modifier = Modifier.height(12.dp))

            Text(
                text = "✓ Acceso ilimitado a todas las instalaciones\n✓ Todas las clases grupales incluidas\n✓ Asesoramiento nutricional gratuito\n✓ Plan de entrenamiento personalizado",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSecondaryContainer.copy(alpha = 0.9f),
                lineHeight = MaterialTheme.typography.bodySmall.lineHeight.times(1.5f)
            )
        }
    }
}

@Composable
fun GymInfoRow(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    title: String,
    content: String
) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.Top
    ) {
        Icon(
            imageVector = icon,
            contentDescription = title,
            modifier = Modifier.size(20.dp),
            tint = MaterialTheme.colorScheme.primary
        )
        Spacer(modifier = Modifier.width(12.dp))
        Column {
            Text(
                text = title,
                style = MaterialTheme.typography.labelMedium,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.onSecondaryContainer.copy(alpha = 0.7f)
            )
            Spacer(modifier = Modifier.height(2.dp))
            Text(
                text = content,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSecondaryContainer
            )
        }
    }
}

@Composable
fun ClasesContent(
    modifier: Modifier = Modifier,
    clases: List<com.example.android.data.model.Clase>,
    isLoading: Boolean,
    errorMessage: String?,
    userRole: com.example.android.data.UserRole,
    onClaseClick: (com.example.android.data.model.Clase) -> Unit,
    onEditClase: (com.example.android.data.model.Clase) -> Unit,
    onDeleteClase: (com.example.android.data.model.Clase) -> Unit,
    onCreateClase: () -> Unit
) {
    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item {
            Spacer(modifier = Modifier.height(8.dp))
        }

        item {
            Column(
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "Todas las Clases",
                        style = MaterialTheme.typography.headlineMedium,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary
                    )

                    // Botón crear clase (solo admin y empleado)
                    if (userRole == com.example.android.data.UserRole.ADMINISTRADOR ||
                        userRole == com.example.android.data.UserRole.EMPLEADO) {
                        FloatingActionButton(
                            onClick = onCreateClase,
                            containerColor = MaterialTheme.colorScheme.primary
                        ) {
                            Icon(
                                imageVector = Icons.Default.Add,
                                contentDescription = "Crear clase",
                                tint = MaterialTheme.colorScheme.onPrimary
                            )
                        }
                    }
                }
            }
        }

        // Estado de carga
        if (isLoading) {
            item {
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(32.dp),
                    contentAlignment = Alignment.Center
                ) {
                    CircularProgressIndicator()
                }
            }
        }

        // Mensaje de error
        errorMessage?.let { error ->
            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.errorContainer
                    )
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp)
                    ) {
                        Row(
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(
                                imageVector = Icons.Default.Error,
                                contentDescription = "Error",
                                tint = MaterialTheme.colorScheme.error
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                text = "Error al cargar las clases",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.onErrorContainer
                            )
                        }
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = error,
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onErrorContainer
                        )
                    }
                }
            }
        }

        // Contador de clases
        if (!isLoading && errorMessage == null) {
            item {
                Text(
                    text = if (clases.isEmpty()) "No hay clases programadas" else "${clases.size} clases disponibles",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }

        // Lista de clases
        items(clases) { clase ->
            ClaseCard(
                clase = clase,
                onClick = { onClaseClick(clase) },
                userRole = userRole,
                onEdit = if (userRole == com.example.android.data.UserRole.ADMINISTRADOR ||
                             userRole == com.example.android.data.UserRole.EMPLEADO) {
                    { onEditClase(clase) }
                } else null,
                onDelete = if (userRole == com.example.android.data.UserRole.ADMINISTRADOR) {
                    { onDeleteClase(clase) }
                } else null
            )
        }

        item {
            Spacer(modifier = Modifier.height(16.dp))
        }
    }
}

@Composable
fun ReservasContent(
    modifier: Modifier = Modifier,
    reservas: List<ReservaCliente>,
    onCancelReserva: (ReservaCliente) -> Unit
) {
    var selectedMonth by remember { mutableStateOf(YearMonth.now()) }
    val reservasPorFecha = remember(reservas) {
        reservas.associateBy { it.fecha }
    }

    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item {
            Spacer(modifier = Modifier.height(8.dp))
        }

        item {
            Text(
                text = "Mis Reservas",
                style = MaterialTheme.typography.headlineMedium,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.primary
            )
        }

        item {
            Text(
                text = "${reservas.size} clases reservadas",
                style = MaterialTheme.typography.bodyLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        item {
            CalendarioReservas(
                selectedMonth = selectedMonth,
                onMonthChange = { selectedMonth = it },
                reservasPorFecha = reservasPorFecha,
                onCancelReserva = onCancelReserva
            )
        }

        item {
            Spacer(modifier = Modifier.height(16.dp))
        }
    }
}

@Composable
fun CalendarioReservas(
    selectedMonth: YearMonth,
    onMonthChange: (YearMonth) -> Unit,
    reservasPorFecha: Map<LocalDate, ReservaCliente>,
    onCancelReserva: (ReservaCliente) -> Unit
) {
    var selectedDate by remember { mutableStateOf<LocalDate?>(null) }

    Card(
        modifier = Modifier.fillMaxWidth(),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp)
        ) {
            // Selector de mes
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                IconButton(onClick = { onMonthChange(selectedMonth.minusMonths(1)) }) {
                    Icon(Icons.Default.ChevronLeft, "Mes anterior")
                }

                Text(
                    text = "${selectedMonth.month.getDisplayName(TextStyle.FULL, Locale.forLanguageTag("es-ES"))} ${selectedMonth.year}",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )

                IconButton(onClick = { onMonthChange(selectedMonth.plusMonths(1)) }) {
                    Icon(Icons.Default.ChevronRight, "Mes siguiente")
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            // Días de la semana
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                listOf("L", "M", "X", "J", "V", "S", "D").forEach { day ->
                    Text(
                        text = day,
                        style = MaterialTheme.typography.labelMedium,
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier.weight(1f),
                        textAlign = TextAlign.Center,
                        color = MaterialTheme.colorScheme.primary
                    )
                }
            }

            Spacer(modifier = Modifier.height(8.dp))

            // Grid del calendario
            CalendarGrid(
                selectedMonth = selectedMonth,
                reservasPorFecha = reservasPorFecha,
                selectedDate = selectedDate,
                onDateClick = { date ->
                    selectedDate = if (selectedDate == date) null else date
                }
            )

            // Mostrar información de la clase seleccionada
            selectedDate?.let { date ->
                reservasPorFecha[date]?.let { reserva ->
                    Spacer(modifier = Modifier.height(16.dp))
                    HorizontalDivider()
                    Spacer(modifier = Modifier.height(16.dp))

                    ReservaDetailCard(
                        reserva = reserva,
                        onCancelReserva = onCancelReserva
                    )
                }
            }
        }
    }
}

@Composable
fun CalendarGrid(
    selectedMonth: YearMonth,
    reservasPorFecha: Map<LocalDate, ReservaCliente>,
    selectedDate: LocalDate?,
    onDateClick: (LocalDate) -> Unit
) {
    val firstDayOfMonth = selectedMonth.atDay(1)
    val firstDayOfWeek = firstDayOfMonth.dayOfWeek.value % 7
    val daysInMonth = selectedMonth.lengthOfMonth()

    LazyVerticalGrid(
        columns = GridCells.Fixed(7),
        modifier = Modifier.height(300.dp),
        verticalArrangement = Arrangement.spacedBy(4.dp),
        horizontalArrangement = Arrangement.spacedBy(4.dp)
    ) {
        // Espacios vacíos antes del primer día
        items(firstDayOfWeek) {
            Spacer(modifier = Modifier.aspectRatio(1f))
        }

        // Días del mes
        items(daysInMonth) { day ->
            val date = selectedMonth.atDay(day + 1)
            val hasReserva = reservasPorFecha.containsKey(date)
            val isSelected = date == selectedDate

            DayCell(
                day = day + 1,
                hasReserva = hasReserva,
                isSelected = isSelected,
                onClick = { if (hasReserva) onDateClick(date) }
            )
        }
    }
}

@Composable
fun DayCell(
    day: Int,
    hasReserva: Boolean,
    isSelected: Boolean,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier
            .aspectRatio(1f)
            .clickable(enabled = hasReserva, onClick = onClick),
        colors = CardDefaults.cardColors(
            containerColor = when {
                isSelected -> MaterialTheme.colorScheme.primary
                hasReserva -> MaterialTheme.colorScheme.secondaryContainer
                else -> MaterialTheme.colorScheme.surfaceVariant
            }
        ),
        elevation = CardDefaults.cardElevation(
            defaultElevation = if (hasReserva) 2.dp else 0.dp
        )
    ) {
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = day.toString(),
                style = MaterialTheme.typography.bodyMedium,
                color = when {
                    isSelected -> MaterialTheme.colorScheme.onPrimary
                    hasReserva -> MaterialTheme.colorScheme.onSecondaryContainer
                    else -> MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.5f)
                },
                fontWeight = if (hasReserva) FontWeight.Bold else FontWeight.Normal
            )
        }
    }
}

@Composable
fun ReservaDetailCard(
    reserva: ReservaCliente,
    onCancelReserva: (ReservaCliente) -> Unit
) {
    Column {
        Text(
            text = "Clase reservada",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold,
            color = MaterialTheme.colorScheme.primary
        )

        Spacer(modifier = Modifier.height(8.dp))

        Text(
            text = reserva.clase.nombre,
            style = MaterialTheme.typography.titleLarge,
            fontWeight = FontWeight.Bold
        )

        Spacer(modifier = Modifier.height(4.dp))

        Row(
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                imageVector = Icons.Default.CalendarToday,
                contentDescription = "Fecha y hora",
                modifier = Modifier.size(16.dp),
                tint = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(modifier = Modifier.width(8.dp))
            Text(
                text = formatFechaHora(reserva.clase.fechaHora),
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        Spacer(modifier = Modifier.height(4.dp))

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
                text = "${reserva.clase.duracionMinutos} minutos",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        Spacer(modifier = Modifier.height(4.dp))

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
                text = reserva.clase.instructor,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        Spacer(modifier = Modifier.height(4.dp))

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
                text = reserva.clase.sala,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        Spacer(modifier = Modifier.height(16.dp))

        // Botón de cancelar reserva
        Button(
            onClick = { onCancelReserva(reserva) },
            modifier = Modifier.fillMaxWidth(),
            colors = ButtonDefaults.buttonColors(
                containerColor = MaterialTheme.colorScheme.error
            )
        ) {
            Icon(
                imageVector = Icons.Default.Cancel,
                contentDescription = "Cancelar reserva",
                modifier = Modifier.size(20.dp)
            )
            Spacer(modifier = Modifier.width(8.dp))
            Text("Cancelar Reserva")
        }
    }
}
