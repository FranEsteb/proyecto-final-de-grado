const express = require('express');
const modelsUsuario = require('../models/modelsUsuario');
const verifyToken = require('../middlewares/authMiddleware');
const DescuentoService = require('../services/descuentoService');

const router = express.Router();

// Función para verificar si el usuario es admin o empleado
function isAuthorized(userRole) {
    return userRole === 'administrador' || userRole === 'empleado';
}

// CALCULAR PRECIO CON DESCUENTOS
router.post('/calcular-precio', verifyToken, async (req, res) => {
    try {
        const { usuarioDni, usuarioId, precioOriginal, tipoTransaccion = 'general' } = req.body;

        if (!precioOriginal || precioOriginal <= 0) {
            return res.status(400).json({ message: "Precio original es requerido y debe ser mayor a 0" });
        }

        // Buscar usuario por DNI o ID
        let usuario;
        if (usuarioId) {
            usuario = await modelsUsuario.findById(usuarioId);
        } else if (usuarioDni) {
            usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        } else {
            return res.status(400).json({ message: "Se requiere usuarioId o usuarioDni" });
        }

        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }

        const calculo = await DescuentoService.calcularPrecioConDescuentos(
            usuario._id, 
            parseFloat(precioOriginal), 
            tipoTransaccion
        );

        res.status(200).json({
            exito: true,
            calculo,
            timestamp: new Date()
        });

    } catch (error) {
        console.error('Error calculando precio:', error);
        res.status(500).json({ message: error.message });
    }
});

// OBTENER RESUMEN DE DESCUENTOS DE UN USUARIO
router.get('/resumen/:usuarioDni', verifyToken, async (req, res) => {
    try {
        const { usuarioDni } = req.params;
        
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }

        const resumen = await DescuentoService.obtenerResumenDescuentos(usuario._id);
        
        res.status(200).json(resumen);

    } catch (error) {
        console.error('Error obteniendo resumen:', error);
        res.status(500).json({ message: error.message });
    }
});

// APLICAR DESCUENTO TEMPORAL A USUARIO
router.post('/aplicar-temporal', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para aplicar descuentos" });
        }

        const { usuarioDni, porcentaje, motivo, duracionDias = 30 } = req.body;

        if (!usuarioDni || !porcentaje || !motivo) {
            return res.status(400).json({ message: "usuarioDni, porcentaje y motivo son requeridos" });
        }

        if (porcentaje < 0 || porcentaje > 50) {
            return res.status(400).json({ message: "El porcentaje debe estar entre 0 y 50" });
        }

        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }

        const resultado = await DescuentoService.aplicarDescuentoTemporal(
            usuario._id,
            parseFloat(porcentaje),
            motivo,
            parseInt(duracionDias),
            req.user.id
        );

        res.status(200).json({
            exito: true,
            mensaje: resultado.mensaje,
            descuento: resultado.descuento,
            validoHasta: resultado.validoHasta
        });

    } catch (error) {
        console.error('Error aplicando descuento temporal:', error);
        res.status(500).json({ message: error.message });
    }
});

// SIMULADOR DE PRECIOS (para testing)
router.post('/simular', verifyToken, async (req, res) => {
    try {
        const { usuarioDni, precios } = req.body;

        if (!usuarioDni || !precios || !Array.isArray(precios)) {
            return res.status(400).json({ message: "usuarioDni y array de precios son requeridos" });
        }

        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) {
            return res.status(404).json({ message: "Usuario no encontrado" });
        }

        const simulaciones = [];

        for (const precio of precios) {
            if (typeof precio.valor !== 'number' || precio.valor <= 0) {
                continue;
            }

            const calculo = await DescuentoService.calcularPrecioConDescuentos(
                usuario._id, 
                precio.valor, 
                precio.tipo || 'general'
            );

            simulaciones.push({
                descripcion: precio.descripcion || `Precio ${precio.valor}`,
                tipo: precio.tipo || 'general',
                ...calculo
            });
        }

        res.status(200).json({
            usuario: {
                nombre: usuario.nombre,
                apellidos: usuario.apellidos,
                dni: usuario.dni,
                reputacion: usuario.reputacion
            },
            simulaciones,
            totalSimulaciones: simulaciones.length
        });

    } catch (error) {
        console.error('Error en simulación:', error);
        res.status(500).json({ message: error.message });
    }
});

// LIMPIAR DESCUENTOS EXPIRADOS (solo admin)
router.post('/limpiar-expirados', verifyToken, async (req, res) => {
    try {
        if (req.user.rol !== 'administrador') {
            return res.status(403).json({ message: "Solo administradores pueden limpiar descuentos expirados" });
        }

        const resultado = await DescuentoService.limpiarDescuentosExpirados();
        
        res.status(200).json(resultado);

    } catch (error) {
        console.error('Error limpiando descuentos expirados:', error);
        res.status(500).json({ message: error.message });
    }
});

// ESTADÍSTICAS DE DESCUENTOS (solo admin/empleado)
router.get('/estadisticas', verifyToken, async (req, res) => {
    try {
        if (!isAuthorized(req.user.rol)) {
            return res.status(403).json({ message: "No tienes permisos para ver estadísticas" });
        }

        // Obtener algunos usuarios de muestra para estadísticas
        const usuariosActivos = await modelsUsuario.find({ 
            'membresia.activa': true 
        }).limit(10);

        const estadisticas = [];

        for (const usuario of usuariosActivos) {
            const resumen = await DescuentoService.obtenerResumenDescuentos(usuario._id);
            estadisticas.push({
                usuario: resumen.usuario,
                descuentoTotal: resumen.resumen.descuentoTotalPorcentaje,
                tieneMembresia: resumen.descuentos.membresia.porcentaje > 0,
                reputacion: resumen.usuario.reputacion
            });
        }

        // Calcular promedios
        const descuentoPromedio = estadisticas.length > 0 ? 
            estadisticas.reduce((sum, stat) => sum + stat.descuentoTotal, 0) / estadisticas.length : 0;

        const reputacionPromedio = estadisticas.length > 0 ?
            estadisticas.reduce((sum, stat) => sum + stat.reputacion, 0) / estadisticas.length : 0;

        res.status(200).json({
            resumen: {
                usuariosAnalizados: estadisticas.length,
                descuentoPromedio: parseFloat(descuentoPromedio.toFixed(1)),
                reputacionPromedio: parseFloat(reputacionPromedio.toFixed(1)),
                conMembresia: estadisticas.filter(s => s.tieneMembresia).length
            },
            detalles: estadisticas
        });

    } catch (error) {
        console.error('Error obteniendo estadísticas:', error);
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;