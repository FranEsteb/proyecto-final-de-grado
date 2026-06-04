const express = require('express');
const modelsMaquina = require('../models/modelsMaquina');
const MaquinaService = require('../services/maquinaService');
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// GET DASHBOARD: Panel de administración con resumen completo
router.get('/dashboard', verifyToken, async (req, res) => {
    try {
        const estadisticas = await MaquinaService.obtenerEstadisticasGenerales();
        const notificaciones = await MaquinaService.obtenerMaquinasConNotificaciones();
        
        const maquinasRecientes = await modelsMaquina.find()
            .sort({ createdAt: -1 })
            .limit(5)
            .populate('proveedor');

        const mantenimientosPendientes = await modelsMaquina.find({
            mantenimientoProgramado: { $lte: new Date(Date.now() + (7 * 24 * 60 * 60 * 1000)) }
        }).select('numeroSerie tipo marca mantenimientoProgramado');

        res.status(200).json({
            estadisticas: estadisticas.success ? estadisticas.data : null,
            notificaciones: notificaciones.success ? notificaciones.data : [],
            maquinasRecientes,
            mantenimientosPendientes
        });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST BULK UPDATE: Actualización masiva de máquinas
router.post('/bulkUpdate', verifyToken, async (req, res) => {
    try {
        const { numerosSerie, updates } = req.body;
        const usuario = req.user?.email || 'Sistema';
        
        if (!numerosSerie || !Array.isArray(numerosSerie) || !updates) {
            return res.status(400).json({ message: 'numerosSerie (array) y updates son requeridos' });
        }

        const resultados = [];
        
        for (const numeroSerie of numerosSerie) {
            try {
                const maquina = await modelsMaquina.findOne({ numeroSerie });
                if (maquina) {
                    Object.keys(updates).forEach(key => {
                        if (key !== 'estado') {
                            maquina[key] = updates[key];
                        }
                    });

                    if (updates.estado && updates.estado !== maquina.estado) {
                        maquina.historialEstados.push({
                            estado: updates.estado,
                            fechaCambio: new Date(),
                            motivo: 'Actualización masiva',
                            usuario: usuario
                        });
                        maquina.estado = updates.estado;
                    }

                    await MaquinaService.actualizarNotificaciones(maquina);
                    await maquina.save();
                    
                    resultados.push({ numeroSerie, success: true });
                } else {
                    resultados.push({ numeroSerie, success: false, error: 'Máquina no encontrada' });
                }
            } catch (error) {
                resultados.push({ numeroSerie, success: false, error: error.message });
            }
        }

        res.status(200).json({
            message: 'Actualización masiva completada',
            resultados
        });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET MAINTENANCE REPORT: Reporte de mantenimientos
router.get('/maintenanceReport', verifyToken, async (req, res) => {
    try {
        const { desde, hasta } = req.query;
        
        let filtroFecha = {};
        if (desde || hasta) {
            filtroFecha.mantenimientoProgramado = {};
            if (desde) filtroFecha.mantenimientoProgramado.$gte = new Date(desde);
            if (hasta) filtroFecha.mantenimientoProgramado.$lte = new Date(hasta);
        }

        const mantenimientos = await modelsMaquina.find(filtroFecha)
            .populate('proveedor')
            .sort({ mantenimientoProgramado: 1 });

        const resumen = {
            total: mantenimientos.length,
            pendientes: mantenimientos.filter(m => m.mantenimientoProgramado > new Date()).length,
            vencidos: mantenimientos.filter(m => m.mantenimientoProgramado <= new Date()).length,
            porTipo: {}
        };

        mantenimientos.forEach(m => {
            resumen.porTipo[m.tipo] = (resumen.porTipo[m.tipo] || 0) + 1;
        });

        res.status(200).json({
            resumen,
            mantenimientos
        });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST EXPORT DATA: Exportar datos de máquinas
router.post('/export', verifyToken, async (req, res) => {
    try {
        const { formato, filtros } = req.body;
        
        const condiciones = {};
        if (filtros) {
            if (filtros.tipo) condiciones.tipo = filtros.tipo;
            if (filtros.estado) condiciones.estado = filtros.estado;
            if (filtros.fechaDesde) condiciones.createdAt = { $gte: new Date(filtros.fechaDesde) };
            if (filtros.fechaHasta) {
                condiciones.createdAt = condiciones.createdAt || {};
                condiciones.createdAt.$lte = new Date(filtros.fechaHasta);
            }
        }

        const maquinas = await modelsMaquina.find(condiciones).populate('proveedor');

        switch (formato) {
            case 'csv':
                const csvData = maquinas.map(m => ({
                    numeroSerie: m.numeroSerie,
                    tipo: m.tipo,
                    marca: m.marca,
                    modelo: m.modelo,
                    estado: m.estado,
                    fechaCompra: m.fechaCompra,
                    ubicacion: m.ubicacion,
                    horasUso: m.horasUso,
                    proveedor: m.proveedor?.nombre || 'N/A',
                    createdAt: m.createdAt
                }));
                
                res.status(200).json({
                    formato: 'csv',
                    datos: csvData,
                    total: csvData.length
                });
                break;
                
            case 'json':
            default:
                res.status(200).json({
                    formato: 'json',
                    datos: maquinas,
                    total: maquinas.length
                });
                break;
        }
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET USAGE ANALYTICS: Análisis de uso de máquinas
router.get('/usageAnalytics', verifyToken, async (req, res) => {
    try {
        const maquinasConUso = await modelsMaquina.find({ horasUso: { $gt: 0 } })
            .select('numeroSerie tipo marca horasUso estado createdAt')
            .sort({ horasUso: -1 });

        const promedioUso = maquinasConUso.reduce((sum, m) => sum + m.horasUso, 0) / maquinasConUso.length;
        
        const usoPorTipo = {};
        maquinasConUso.forEach(m => {
            if (!usoPorTipo[m.tipo]) {
                usoPorTipo[m.tipo] = { total: 0, count: 0, promedio: 0 };
            }
            usoPorTipo[m.tipo].total += m.horasUso;
            usoPorTipo[m.tipo].count += 1;
        });

        Object.keys(usoPorTipo).forEach(tipo => {
            usoPorTipo[tipo].promedio = usoPorTipo[tipo].total / usoPorTipo[tipo].count;
        });

        res.status(200).json({
            promedioGeneral: Math.round(promedioUso * 100) / 100,
            totalMaquinasConUso: maquinasConUso.length,
            usoPorTipo,
            maquinasMasUsadas: maquinasConUso.slice(0, 10),
            maquinasMenosUsadas: maquinasConUso.slice(-10).reverse()
        });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST BATCH DELETE: Eliminación masiva de máquinas
router.post('/batchDelete', verifyToken, async (req, res) => {
    try {
        const { numerosSerie, confirmarEliminacion } = req.body;
        
        if (!numerosSerie || !Array.isArray(numerosSerie)) {
            return res.status(400).json({ message: 'numerosSerie debe ser un array' });
        }

        if (!confirmarEliminacion) {
            return res.status(400).json({ 
                message: 'Debe confirmar la eliminación estableciendo confirmarEliminacion: true' 
            });
        }

        const resultados = [];
        
        for (const numeroSerie of numerosSerie) {
            try {
                const resultado = await modelsMaquina.deleteOne({ numeroSerie });
                if (resultado.deletedCount > 0) {
                    resultados.push({ numeroSerie, eliminada: true });
                } else {
                    resultados.push({ numeroSerie, eliminada: false, motivo: 'No encontrada' });
                }
            } catch (error) {
                resultados.push({ numeroSerie, eliminada: false, motivo: error.message });
            }
        }

        res.status(200).json({
            message: 'Eliminación masiva completada',
            resultados,
            totalEliminadas: resultados.filter(r => r.eliminada).length
        });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;