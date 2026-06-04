const express = require('express');
const modelsMembresia = require('../models/modelsMembresia');
const modelsUsuario = require('../models/modelsUsuario');
const verifyToken = require('../middlewares/authMiddleware');
const MembresiaService = require('../services/membresiaService');

const router = express.Router();

// Función para verificar si el usuario es admin o empleado
function isAuthorized(userRole) {
    return userRole === 'administrador' || userRole === 'empleado';
}

// CREAR NUEVA MEMBRESÍA
router.post('/crear', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para crear membresías" });
        }

        const { usuarioId, usuarioDni, tipo, duracionMeses, fechaInicio, fechaFin } = req.body;

        console.log('Creando membresía - datos recibidos:', { usuarioId, usuarioDni, tipo, duracionMeses, fechaInicio, fechaFin });

        if (!usuarioId && !usuarioDni) {
            return res.status(400).json({ message: "usuarioId o usuarioDni es requerido" });
        }

        // Verificar que el usuario existe (buscar por ID o DNI)
        let usuario;
        if (usuarioId) {
            usuario = await modelsUsuario.findById(usuarioId);
        } else if (usuarioDni) {
            usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        }

        console.log('Usuario encontrado:', usuario ? `${usuario.nombre} ${usuario.apellidos}` : 'No encontrado');

        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }

        // Verificar que no tenga una membresía activa
        const membresiaExistente = await modelsMembresia.obtenerMembresiaActiva(usuario._id);
        if (membresiaExistente) {
            return res.status(400).json({ message: "El usuario ya tiene una membresía activa" });
        }

        const inicioMembresia = fechaInicio ? new Date(fechaInicio) : new Date();

        // Si se proporciona fechaFin directa, usarla; si no, calcular desde duracionMeses
        let finMembresia;
        if (fechaFin) {
            finMembresia = new Date(fechaFin);
        } else {
            finMembresia = new Date(inicioMembresia);
            finMembresia.setMonth(finMembresia.getMonth() + (duracionMeses || 12));
        }

        const membresia = new modelsMembresia({
            usuario: usuario._id,
            tipo: tipo || 'basica',
            fechaInicio: inicioMembresia,
            fechaFin: finMembresia,
            estado: 'activa'
        });

        const savedMembresia = await membresia.save();

        // Calcular descuento inicial por permanencia
        const descuentoInicial = savedMembresia.calcularDescuentoPorPermanencia();
        savedMembresia.descuentoActual = descuentoInicial;
        await savedMembresia.save();

        // Actualizar el usuario con la información de membresía
        await modelsUsuario.findByIdAndUpdate(usuario._id, {
            'membresia.activa': true,
            'membresia.tipo': tipo || 'basica',
            'membresia.fechaInicio': inicioMembresia,
            'membresia.fechaFin': finMembresia,
            'membresia.descuentoPorPermanencia': descuentoInicial
        });

        res.status(201).json({
            message: "Membresía creada exitosamente",
            membresia: savedMembresia
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// OBTENER MEMBRESÍA DE UN USUARIO
router.get('/usuario/:usuarioId', verifyToken, async (req, res) => {
    try {
        const { usuarioId } = req.params;

        // Primero buscar el usuario para obtener su _id real
        let usuario;
        try {
            // Intentar buscar por ObjectId
            usuario = await modelsUsuario.findById(usuarioId);
        } catch (error) {
            // Si falla, buscar por DNI
            usuario = await modelsUsuario.findOne({ dni: usuarioId });
        }

        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }

        const membresia = await modelsMembresia.obtenerMembresiaActiva(usuario._id);

        if (!membresia) {
            return res.status(404).json({ message: "No se encontró membresía activa" });
        }

        // Calcular descuento actual por permanencia
        const descuentoPermanencia = membresia.calcularDescuentoPorPermanencia();
        const tiempoRestante = membresia.obtenerTiempoRestante();

        res.status(200).json({
            membresia,
            descuentoActual: descuentoPermanencia,
            tiempoRestante,
            proximaAVencer: membresia.estaProximaAVencer()
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// APLICAR DESCUENTO MANUAL
router.post('/aplicar-descuento', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para aplicar descuentos" });
        }

        const { usuarioId, usuarioDni, porcentaje, motivo, duracionDias } = req.body;

        if (!porcentaje || !motivo) {
            return res.status(400).json({ message: "Porcentaje y motivo son requeridos" });
        }

        if (porcentaje < 0 || porcentaje > 50) {
            return res.status(400).json({ message: "El porcentaje debe estar entre 0 y 50" });
        }

        // Obtener el usuario primero
        let usuario;
        if (usuarioId) {
            usuario = await modelsUsuario.findById(usuarioId);
        } else if (usuarioDni) {
            usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        }

        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }

        const membresia = await modelsMembresia.obtenerMembresiaActiva(usuario._id);
        if (!membresia) {
            return res.status(404).json({ message: "No se encontró membresía activa para este usuario" });
        }

        // Agregar al historial de descuentos
        membresia.historialDescuentos.push({
            porcentaje,
            motivo,
            aplicadoPor: req.user.id
        });

        // IMPORTANTE: Actualizar el campo descuentoActual en la membresía
        membresia.descuentoActual = porcentaje;

        await membresia.save();

        // Aplicar descuento temporal al usuario (ya tenemos el objeto usuario)
        const fechaInicio = new Date();
        const fechaFin = new Date();
        fechaFin.setDate(fechaFin.getDate() + (duracionDias || 30));

        usuario.descuentosActivos.push({
            tipo: `Descuento manual - ${motivo}`,
            porcentaje,
            fechaInicio,
            fechaFin
        });

        // IMPORTANTE: Actualizar también el campo membresia.descuentoPorPermanencia en el usuario
        usuario.membresia.descuentoPorPermanencia = porcentaje;

        await usuario.save();

        res.status(200).json({
            message: "Descuento aplicado exitosamente",
            descuento: {
                porcentaje,
                motivo,
                fechaInicio,
                fechaFin
            }
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// RENOVAR MEMBRESÍA
router.post('/renovar/:membresiaId', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para renovar membresías" });
        }

        const { membresiaId } = req.params;
        const { duracionMeses, nuevoTipo } = req.body;

        const membresia = await modelsMembresia.findById(membresiaId);
        if (!membresia) {
            return res.status(404).json({ message: "Membresía no encontrada" });
        }

        const nuevaFechaFin = new Date(membresia.fechaFin);
        nuevaFechaFin.setMonth(nuevaFechaFin.getMonth() + (duracionMeses || 12));

        membresia.fechaFin = nuevaFechaFin;
        membresia.estado = 'activa';
        
        if (nuevoTipo) {
            membresia.tipo = nuevoTipo;
        }

        await membresia.save();

        // Actualizar usuario
        await modelsUsuario.findByIdAndUpdate(membresia.usuario, {
            'membresia.fechaFin': nuevaFechaFin,
            'membresia.tipo': membresia.tipo,
            'membresia.activa': true
        });

        res.status(200).json({
            message: "Membresía renovada exitosamente",
            membresia
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// OBTENER ESTADÍSTICAS DE MEMBRESÍAS
router.get('/estadisticas', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para ver estadísticas" });
        }

        const estadisticas = await modelsMembresia.aggregate([
            {
                $group: {
                    _id: "$estado",
                    count: { $sum: 1 }
                }
            }
        ]);

        const estadisticasPorTipo = await modelsMembresia.aggregate([
            {
                $match: { estado: 'activa' }
            },
            {
                $group: {
                    _id: "$tipo",
                    count: { $sum: 1 }
                }
            }
        ]);

        const proximasAVencer = await modelsMembresia.find({
            estado: 'activa',
            fechaFin: {
                $gte: new Date(),
                $lte: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000) // Próximos 7 días
            }
        }).populate('usuario', 'nombre apellidos email');

        res.status(200).json({
            estadisticasGenerales: estadisticas,
            estadisticasPorTipo,
            proximasAVencer: proximasAVencer.length,
            detalleProximasAVencer: proximasAVencer
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// ACTUALIZAR DESCUENTOS POR PERMANENCIA (Tarea automática)
router.post('/actualizar-descuentos-permanencia', verifyToken, async (req, res) => {
    try {
        if (req.user.rol !== 'administrador') {
            return res.status(403).json({ message: "Solo administradores pueden ejecutar esta tarea" });
        }

        const resultado = await MembresiaService.actualizarDescuentosPermanencia();
        
        res.status(200).json({
            message: resultado.mensaje,
            total: resultado.total,
            actualizadas: resultado.actualizadas
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// EJECUTAR MANTENIMIENTO COMPLETO
router.post('/mantenimiento', verifyToken, async (req, res) => {
    try {
        if (req.user.rol !== 'administrador') {
            return res.status(403).json({ message: "Solo administradores pueden ejecutar mantenimiento" });
        }

        const resultado = await MembresiaService.ejecutarMantenimiento();
        
        res.status(200).json({
            message: "Mantenimiento ejecutado exitosamente",
            resultado
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// PAUSAR MEMBRESÍA
router.post('/pausar/:membresiaId', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para pausar membresías" });
        }

        const { membresiaId } = req.params;
        const membresia = await modelsMembresia.findById(membresiaId);
        
        if (!membresia) {
            return res.status(404).json({ message: "Membresía no encontrada" });
        }

        if (membresia.estado !== 'activa') {
            return res.status(400).json({ message: "Solo se pueden pausar membresías activas" });
        }

        membresia.estado = 'pausada';
        await membresia.save();

        // Actualizar usuario
        await modelsUsuario.findByIdAndUpdate(membresia.usuario, {
            'membresia.activa': false
        });

        res.status(200).json({
            message: "Membresía pausada exitosamente",
            membresia
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// REACTIVAR MEMBRESÍA PAUSADA
router.post('/reactivar/:membresiaId', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para reactivar membresías" });
        }

        const { membresiaId } = req.params;
        const membresia = await modelsMembresia.findById(membresiaId);
        
        if (!membresia) {
            return res.status(404).json({ message: "Membresía no encontrada" });
        }

        if (membresia.estado !== 'pausada') {
            return res.status(400).json({ message: "Solo se pueden reactivar membresías pausadas" });
        }

        // Verificar que no haya expirado
        if (membresia.fechaFin <= new Date()) {
            return res.status(400).json({ message: "No se puede reactivar una membresía expirada" });
        }

        membresia.estado = 'activa';
        await membresia.save();

        // Actualizar usuario
        const descuentoActual = membresia.calcularDescuentoPorPermanencia();
        await modelsUsuario.findByIdAndUpdate(membresia.usuario, {
            'membresia.activa': true,
            'membresia.descuentoPorPermanencia': descuentoActual
        });

        res.status(200).json({
            message: "Membresía reactivada exitosamente",
            membresia
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// ELIMINAR MEMBRESÍA
router.delete('/eliminar/:membresiaId', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para eliminar membresías" });
        }

        const { membresiaId } = req.params;
        const membresia = await modelsMembresia.findById(membresiaId);
        
        if (!membresia) {
            return res.status(404).json({ message: "Membresía no encontrada" });
        }

        // Marcar como cancelada en lugar de eliminar (para mantener historial)
        membresia.estado = 'cancelada';
        await membresia.save();

        // Actualizar usuario
        await modelsUsuario.findByIdAndUpdate(membresia.usuario, {
            'membresia.activa': false,
            'membresia.descuentoPorPermanencia': 0
        });

        res.status(200).json({
            message: "Membresía cancelada exitosamente"
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// MODIFICAR MEMBRESÍA
router.put('/modificar/:membresiaId', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para modificar membresías" });
        }

        const { membresiaId } = req.params;
        const { tipo, fechaFin, duracionMesesExtra } = req.body;

        const membresia = await modelsMembresia.findById(membresiaId);
        
        if (!membresia) {
            return res.status(404).json({ message: "Membresía no encontrada" });
        }

        if (membresia.estado === 'cancelada') {
            return res.status(400).json({ message: "No se puede modificar una membresía cancelada" });
        }

        // Actualizar campos si se proporcionan
        if (tipo && ['basica', 'premium', 'vip'].includes(tipo)) {
            membresia.tipo = tipo;
        }

        if (fechaFin) {
            membresia.fechaFin = new Date(fechaFin);
        } else if (duracionMesesExtra) {
            const nuevaFechaFin = new Date(membresia.fechaFin);
            nuevaFechaFin.setMonth(nuevaFechaFin.getMonth() + duracionMesesExtra);
            membresia.fechaFin = nuevaFechaFin;
        }

        // Reactivar si estaba pausada y no ha expirado
        if (membresia.estado === 'pausada' && membresia.fechaFin > new Date()) {
            membresia.estado = 'activa';
        }

        // Recalcular descuento
        const nuevoDescuento = membresia.calcularDescuentoPorPermanencia();
        membresia.descuentoActual = nuevoDescuento;

        await membresia.save();

        // Actualizar usuario
        await modelsUsuario.findByIdAndUpdate(membresia.usuario, {
            'membresia.tipo': membresia.tipo,
            'membresia.fechaFin': membresia.fechaFin,
            'membresia.activa': membresia.estado === 'activa',
            'membresia.descuentoPorPermanencia': nuevoDescuento
        });

        res.status(200).json({
            message: "Membresía modificada exitosamente",
            membresia
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// BUSCAR MEMBRESÍA POR DNI O ID DE USUARIO
router.get('/buscar-por-usuario', verifyToken, async (req, res) => {
    try {
        const { usuarioId, usuarioDni } = req.query;

        if (!usuarioId && !usuarioDni) {
            return res.status(400).json({ message: "Se requiere usuarioId o usuarioDni" });
        }

        // Buscar usuario
        let usuario;
        if (usuarioId) {
            try {
                usuario = await modelsUsuario.findById(usuarioId);
            } catch (error) {
                // Si falla por ID inválido, intentar por DNI
                usuario = await modelsUsuario.findOne({ dni: usuarioId });
            }
        } else if (usuarioDni) {
            usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        }

        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }

        // Buscar todas las membresías del usuario (activas, pausadas, expiradas)
        const membresias = await modelsMembresia.find({ 
            usuario: usuario._id 
        }).sort({ fechaInicio: -1 });

        res.status(200).json({
            usuario: {
                id: usuario._id,
                nombre: usuario.nombre,
                apellidos: usuario.apellidos,
                dni: usuario.dni,
                email: usuario.email
            },
            membresias
        });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;