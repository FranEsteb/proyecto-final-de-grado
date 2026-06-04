const express = require('express');
const modelsReputacion = require('../models/modelsReputacion');
const modelsUsuario = require('../models/modelsUsuario');
const verifyToken = require('../middlewares/authMiddleware');
const ReputacionService = require('../services/reputacionService');

const router = express.Router();

// GET ALL: Obtener todas las entradas de reputación
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const reputaciones = await modelsReputacion.find().populate('usuario');
        res.status(200).json(reputaciones);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST NEW: Crear nueva entrada de reputación con usuarioDni (ACTUALIZADO)
router.post('/new', verifyToken, async (req, res) => {
    try {
        const { usuarioDni, tipo, motivo, puntos } = req.body;

        console.log(`🔄 Aplicando reputación: ${tipo} ${puntos} puntos a DNI ${usuarioDni}`);

        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) {
            console.log(`❌ Usuario no encontrado con DNI: ${usuarioDni}`);
            return res.status(404).json({ message: "Usuario no encontrado con ese DNI" });
        }

        console.log(`✅ Usuario encontrado: ${usuario.nombre} ${usuario.apellidos}, reputación actual: ${usuario.reputacion}`);

        // Usar el servicio para aplicar cambio de reputación
        const resultado = await ReputacionService.aplicarCambioReputacion(
            usuario._id, 
            tipo, 
            motivo, 
            puntos,
            req.user.id // Quien aplica el cambio
        );

        // Verificar que se actualizó correctamente
        const usuarioActualizado = await modelsUsuario.findById(usuario._id);
        console.log(`🔍 Verificación: Usuario ${usuarioActualizado.nombre} ahora tiene ${usuarioActualizado.reputacion} puntos`);

        res.status(200).json({
            mensaje: resultado.mensaje,
            registroCreado: resultado.registroCreado,
            nuevaReputacion: resultado.nuevaReputacion,
            usuarioActualizado: {
                nombre: usuarioActualizado.nombre,
                apellidos: usuarioActualizado.apellidos,
                reputacionAnterior: usuario.reputacion,
                reputacionNueva: usuarioActualizado.reputacion
            }
        });
    } catch (error) {
        console.error('❌ Error en endpoint /new:', error);
        res.status(400).json({ message: error.message });
    }
});

// PATCH UPDATE: Actualizar entrada por idRep
router.patch('/update', verifyToken, async (req, res) => {
    try {
        const { idRep, usuarioDni, tipo, motivo, puntos } = req.body;
        if (!idRep) return res.status(400).json({ message: "Falta el campo idRep" });

        const updateFields = {};
        if (usuarioDni) {
            const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
            if (!usuario) return res.status(404).json({ message: "Usuario no encontrado con ese DNI" });
            updateFields.usuario = usuario._id;
        }

        if (tipo !== undefined) updateFields.tipo = tipo;
        if (motivo !== undefined) updateFields.motivo = motivo;
        if (puntos !== undefined) updateFields.puntos = puntos;

        const result = await modelsReputacion.updateOne({ idRep }, { $set: updateFields });
        if (result.modifiedCount === 0)
            return res.status(404).json({ message: "Entrada no encontrada o sin cambios" });

        res.status(200).json({ message: "Entrada de reputación actualizada" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE: Eliminar entrada por idRep
router.delete('/delete', verifyToken, async (req, res) => {
    try {
        const { idRep } = req.body;
        const result = await modelsReputacion.deleteOne({ idRep });
        if (result.deletedCount === 0)
            return res.status(404).json({ message: "Entrada no encontrada" });

        res.status(200).json({ message: `Entrada con ID ${idRep} eliminada` });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// GET ONE: Obtener entrada de reputación por idRep
router.post('/getOne', verifyToken, async (req, res) => {
    try {
        const { idRep } = req.body;
        const reputacion = await modelsReputacion.findOne({ idRep }).populate('usuario');
        if (!reputacion)
            return res.status(404).json({ message: "Entrada no encontrada" });

        res.status(200).json(reputacion);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// NUEVAS RUTAS CON SERVICIOS

// Aplicar eventos automáticos de reputación
router.post('/evento/completar-reserva', verifyToken, async (req, res) => {
    try {
        const { usuarioDni, esATime = true } = req.body;
        
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });

        const resultado = await ReputacionService.eventoCompletarReserva(usuario._id, esATime);
        
        res.status(200).json(resultado);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.post('/evento/cancelar-reserva', verifyToken, async (req, res) => {
    try {
        const { usuarioDni, horasAnticipacion } = req.body;
        
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });

        const resultado = await ReputacionService.eventoCancelarReserva(usuario._id, horasAnticipacion);
        
        res.status(200).json(resultado);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

router.post('/evento/no-show', verifyToken, async (req, res) => {
    try {
        const { usuarioDni } = req.body;
        
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });

        const resultado = await ReputacionService.eventoNoShow(usuario._id);
        
        res.status(200).json(resultado);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// Obtener historial de reputación de un usuario
router.get('/historial/:usuarioDni', verifyToken, async (req, res) => {
    try {
        const { usuarioDni } = req.params;
        
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });

        const historial = await ReputacionService.obtenerHistorialReputacion(usuario._id);
        
        res.status(200).json(historial);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// Sincronizar todas las reputaciones (solo admin)
router.post('/sincronizar', verifyToken, async (req, res) => {
    try {
        if (req.user.rol !== 'administrador') {
            return res.status(403).json({ message: "Solo administradores pueden sincronizar reputaciones" });
        }

        const resultado = await ReputacionService.sincronizarTodasLasReputaciones();
        
        res.status(200).json(resultado);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// RUTA DE PRUEBA: Actualización directa de reputación (solo para testing)
router.post('/test-update', verifyToken, async (req, res) => {
    try {
        const { usuarioDni, nuevaReputacion } = req.body;
        
        console.log(`🧪 PRUEBA: Actualizando reputación de ${usuarioDni} a ${nuevaReputacion}`);
        
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }
        
        console.log(`📋 Usuario antes: ${usuario.nombre} ${usuario.apellidos}, reputación: ${usuario.reputacion}`);
        
        // Actualización directa
        const resultado = await modelsUsuario.findByIdAndUpdate(
            usuario._id, 
            { reputacion: nuevaReputacion },
            { new: true } // Devuelve el documento actualizado
        );
        
        console.log(`✅ Usuario después: ${resultado.nombre} ${resultado.apellidos}, reputación: ${resultado.reputacion}`);
        
        // Verificar con una nueva consulta
        const verificacion = await modelsUsuario.findById(usuario._id);
        console.log(`🔍 Verificación independiente: reputación = ${verificacion.reputacion}`);
        
        res.status(200).json({
            mensaje: "Reputación actualizada directamente",
            usuarioAntes: {
                nombre: usuario.nombre,
                reputacion: usuario.reputacion
            },
            usuarioDespues: {
                nombre: resultado.nombre,
                reputacion: resultado.reputacion
            },
            verificacion: {
                reputacion: verificacion.reputacion
            }
        });
        
    } catch (error) {
        console.error('❌ Error en test-update:', error);
        res.status(500).json({ message: error.message });
    }
});

// RUTA PARA VER REPUTACIÓN ACTUAL DE UN USUARIO
router.get('/ver-reputacion/:usuarioDni', verifyToken, async (req, res) => {
    try {
        const { usuarioDni } = req.params;
        
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }
        
        // Calcular también desde los registros
        const reputacionCalculada = await ReputacionService.calcularReputacionTotal(usuario._id);
        
        // Obtener registros de reputación
        const registros = await modelsReputacion.find({ usuario: usuario._id }).sort({ fecha: -1 }).limit(10);
        
        res.status(200).json({
            usuario: {
                dni: usuario.dni,
                nombre: usuario.nombre,
                apellidos: usuario.apellidos,
                reputacionEnBD: usuario.reputacion,
                reputacionCalculada: reputacionCalculada,
                coinciden: usuario.reputacion === reputacionCalculada
            },
            ultimosRegistros: registros.map(r => ({
                fecha: r.fecha,
                tipo: r.tipo,
                motivo: r.motivo,
                puntos: r.puntos
            }))
        });
        
    } catch (error) {
        console.error('❌ Error consultando reputación:', error);
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;
