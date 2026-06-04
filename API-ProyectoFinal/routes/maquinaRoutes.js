const express = require('express');
const modelsMaquina = require('../models/modelsMaquina');
const modelsCosto = require('../models/modelsCosto');
const MaquinaService = require('../services/maquinaService');
const verifyToken = require('../middlewares/authMiddleware');
const {
    validarRegistroMaquina,
    validarActualizacionEstado,
    validarHorasUso,
    validarProgramacionMantenimiento,
    sanitizarBusqueda,
    limitarResultados,
    validarObtenerHistorial
} = require('../middlewares/maquinaValidation');

const router = express.Router();

// GET ALL: Obtener todas las máquinas (WPF)
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        // Obtener máquinas con populate del proveedor para mostrar el nombre
        const maquinas = await modelsMaquina.find()
            .where('numeroSerie').exists(true).ne('')
            .where('tipo').exists(true).ne('')
            .populate('proveedor', 'nombre email telefono direccion cif productosSuministrados')
            .exec();

        // Calcular costos dinámicamente desde la tabla de costos
        const resultado = await Promise.all(maquinas.map(async (m) => {
            const doc = m.toObject();

            // Calcular costos reales desde la tabla de costos
            try {
                const costos = await modelsCosto.find({ maquina: m._id });

                const costoReparacion = costos
                    .filter(c => c.tipoCosto === 'Reparacion')
                    .reduce((sum, c) => sum + c.monto, 0);

                const costoMantenimiento = costos
                    .filter(c => c.tipoCosto === 'Mantenimiento')
                    .reduce((sum, c) => sum + c.monto, 0);

                // Actualizar los costos con los valores calculados
                doc.costoReparacion = costoReparacion;
                doc.costoMantenimiento = costoMantenimiento;

            } catch (costoError) {
                console.error(`Error calculando costos para máquina ${m.numeroSerie}:`, costoError.message);
                // Mantener valores por defecto si hay error
            }

            return doc;
        }));

        res.status(200).json(resultado);
    } catch (error) {
        console.error('Error en getAll:', error);
        res.status(500).json({ message: error.message });
    }
});

// ADMIN: Ver datos RAW de máquinas sin procesamiento
router.get('/admin/raw', verifyToken, async (req, res) => {
    try {
        const maquinas = await modelsMaquina.find().exec();
        const datos = maquinas.map(m => {
            const obj = m.toObject();
            return {
                _id: obj._id,
                numeroSerie: obj.numeroSerie,
                tipo: obj.tipo,
                proveedor: obj.proveedor,
                keys: Object.keys(obj)
            };
        });
        res.status(200).json(datos);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// ADMIN: Diagnosticar y limpiar máquinas con datos inválidos
router.get('/admin/diagnose', verifyToken, async (req, res) => {
    try {
        const maquinas = await modelsMaquina.find();

        const diagnostico = {
            totalMaquinas: maquinas.length,
            maquinasValidas: 0,
            maquinasInvalidas: 0,
            detalles: []
        };

        for (let maquina of maquinas) {
            try {
                if (!maquina.numeroSerie || !maquina.tipo) {
                    diagnostico.maquinasInvalidas++;
                    diagnostico.detalles.push({
                        _id: maquina._id,
                        numeroSerie: maquina.numeroSerie,
                        razon: 'Campos requeridos faltantes'
                    });
                    // Eliminar máquinas inválidas
                    await modelsMaquina.deleteOne({ _id: maquina._id });
                } else {
                    diagnostico.maquinasValidas++;
                }
            } catch (e) {
                diagnostico.maquinasInvalidas++;
                diagnostico.detalles.push({
                    _id: maquina._id,
                    error: e.message
                });
            }
        }

        res.status(200).json(diagnostico);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST NEW: Crear nueva máquina (WPF)
router.post('/new', verifyToken, async (req, res) => {
    try {
        const nuevaMaquina = new modelsMaquina({
            numeroSerie: req.body.numeroSerie,
            tipo: req.body.tipo,
            modelo: req.body.modelo,
            marca: req.body.marca,
            fechaCompra: req.body.fechaCompra,
            estado: req.body.estado || 'operativa',
            proveedor: req.body.proveedor || null, // ID del proveedor (puede ser null)
            ubicacion: req.body.ubicacion,
            mantenimientoProgramado: req.body.mantenimientoProgramado,
            ultimoMantenimiento: req.body.ultimoMantenimiento,
            imagenMaquina: req.body.imagenMaquina,
            horasUso: req.body.horasUso || 0,
            costoCompra: req.body.costoCompra,
            costoReparacion: req.body.costoReparacion || 0,
            costoMantenimiento: req.body.costoMantenimiento || 0,
            especificaciones: req.body.especificaciones || {},
            garantia: req.body.garantia || {}
        });

        const savedMaquina = await nuevaMaquina.save();
        // Retornar la máquina sin populate para evitar problemas de serialización
        const resultado = savedMaquina.toObject();
        res.status(200).json(resultado);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// PATCH UPDATE: Actualizar máquina por numeroSerie (WPF)
router.patch('/update', verifyToken, async (req, res) => {
    try {
        const { numeroSerie } = req.body;
        if (!numeroSerie) return res.status(400).json({ message: "Falta el campo numeroSerie" });

        const updateFields = {};

        if (req.body.tipo !== undefined) updateFields.tipo = req.body.tipo;
        if (req.body.modelo !== undefined) updateFields.modelo = req.body.modelo;
        if (req.body.marca !== undefined) updateFields.marca = req.body.marca;
        if (req.body.fechaCompra !== undefined) updateFields.fechaCompra = req.body.fechaCompra;
        if (req.body.estado !== undefined) updateFields.estado = req.body.estado;
        if (req.body.proveedor !== undefined) updateFields.proveedor = req.body.proveedor;
        if (req.body.ubicacion !== undefined) updateFields.ubicacion = req.body.ubicacion;
        if (req.body.mantenimientoProgramado !== undefined) updateFields.mantenimientoProgramado = req.body.mantenimientoProgramado;
        if (req.body.ultimoMantenimiento !== undefined) updateFields.ultimoMantenimiento = req.body.ultimoMantenimiento;
        if (req.body.imagenMaquina !== undefined) updateFields.imagenMaquina = req.body.imagenMaquina;
        if (req.body.horasUso !== undefined) updateFields.horasUso = req.body.horasUso;
        if (req.body.costoCompra !== undefined) updateFields.costoCompra = req.body.costoCompra;
        if (req.body.especificaciones !== undefined) updateFields.especificaciones = req.body.especificaciones;
        if (req.body.garantia !== undefined) updateFields.garantia = req.body.garantia;

        // Utilizar findOneAndUpdate para obtener el documento actualizado
        const updatedMachine = await modelsMaquina.findOneAndUpdate(
            { numeroSerie },
            { $set: updateFields },
            { new: true }
        ).exec();

        if (!updatedMachine) return res.status(404).json({ message: "Máquina no encontrada o datos sin cambios" });

        // Retornar sin populate para evitar problemas de serialización
        const resultado = updatedMachine.toObject();
        res.status(200).json(resultado);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE: Eliminar máquina por numeroSerie (WPF)
router.delete('/delete', verifyToken, async (req, res) => {
    try {
        const { numeroSerie } = req.body;
        const result = await modelsMaquina.deleteOne({ numeroSerie });
        if (result.deletedCount === 0) return res.status(404).json({ message: "Máquina no encontrada" });

        res.status(200).json({ message: `Máquina con numeroSerie ${numeroSerie} eliminada` });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// GET ONE: Obtener máquina por numeroSerie (WPF)
router.post('/getOne', verifyToken, async (req, res) => {
    try {
        const maquina = await modelsMaquina.findOne({ numeroSerie: req.body.numeroSerie }).exec();
        if (!maquina) return res.status(404).json({ message: "Máquina no encontrada" });

        // Retornar sin populate para evitar problemas de serialización
        const resultado = maquina.toObject();
        res.status(200).json(resultado);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST FILTER: Filtrar máquinas por criterios (WPF)
router.post('/getFilter', verifyToken, async (req, res) => {
    try {
        const condiciones = {};
        if (req.body.tipo) condiciones.tipo = req.body.tipo;
        if (req.body.marca) condiciones.marca = req.body.marca;
        if (req.body.estado) condiciones.estado = req.body.estado;
        if (req.body.ubicacion) condiciones.ubicacion = req.body.ubicacion;

        const maquinas = await modelsMaquina.find(condiciones).populate('proveedor');
        if (maquinas.length === 0) return res.status(404).json({ message: "No se encontraron máquinas" });

        res.status(200).json(maquinas);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST REGISTER: Registro completo de máquina con seguimiento
router.post('/register', verifyToken, validarRegistroMaquina, async (req, res) => {
    try {
        const usuario = req.user?.email || 'Sistema';
        const resultado = await MaquinaService.registrarMaquina(req.body, usuario);
        
        if (resultado.success) {
            res.status(201).json({
                message: 'Máquina registrada exitosamente',
                data: resultado.data
            });
        } else {
            res.status(400).json({ message: resultado.error });
        }
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// PATCH UPDATE STATE: Actualizar estado de máquina con historial
router.patch('/updateState', verifyToken, validarActualizacionEstado, async (req, res) => {
    try {
        const { numeroSerie, estado, motivo, costoReparacion, costoMantenimiento } = req.body;
        const usuario = req.user?.email || 'Sistema';

        if (!numeroSerie || !estado) {
            return res.status(400).json({ message: 'NumeroSerie y estado son requeridos' });
        }

        const resultado = await MaquinaService.actualizarEstado(
            numeroSerie,
            estado,
            motivo,
            usuario,
            costoReparacion || 0,
            costoMantenimiento || 0
        );

        if (resultado.success) {
            res.status(200).json({
                message: 'Estado actualizado exitosamente',
                data: resultado.data
            });
        } else {
            res.status(400).json({ message: resultado.error });
        }
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST USAGE: Registrar horas de uso
router.post('/registerUsage', verifyToken, validarHorasUso, async (req, res) => {
    try {
        const { numeroSerie, horasUso } = req.body;
        
        if (!numeroSerie || !horasUso) {
            return res.status(400).json({ message: 'NumeroSerie y horasUso son requeridos' });
        }
        
        const resultado = await MaquinaService.registrarHorasUso(numeroSerie, horasUso);
        
        if (resultado.success) {
            res.status(200).json({
                message: 'Horas de uso registradas',
                data: resultado.data
            });
        } else {
            res.status(400).json({ message: resultado.error });
        }
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET NOTIFICATIONS: Obtener máquinas con notificaciones
router.get('/notifications', verifyToken, async (req, res) => {
    try {
        const resultado = await MaquinaService.obtenerMaquinasConNotificaciones();
        
        if (resultado.success) {
            res.status(200).json(resultado.data);
        } else {
            res.status(500).json({ message: resultado.error });
        }
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET STATS: Obtener estadísticas generales
router.get('/statistics', verifyToken, async (req, res) => {
    try {
        const resultado = await MaquinaService.obtenerEstadisticasGenerales();
        
        if (resultado.success) {
            res.status(200).json(resultado.data);
        } else {
            res.status(500).json({ message: resultado.error });
        }
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST SCHEDULE MAINTENANCE: Programar mantenimiento
router.post('/scheduleMaintenance', verifyToken, validarProgramacionMantenimiento, async (req, res) => {
    try {
        const { numeroSerie, fechaMantenimiento } = req.body;
        const usuario = req.user?.email || 'Sistema';
        
        if (!numeroSerie || !fechaMantenimiento) {
            return res.status(400).json({ message: 'NumeroSerie y fechaMantenimiento son requeridos' });
        }
        
        const resultado = await MaquinaService.programarMantenimiento(numeroSerie, fechaMantenimiento, usuario);
        
        if (resultado.success) {
            res.status(200).json({
                message: 'Mantenimiento programado exitosamente',
                data: resultado.data
            });
        } else {
            res.status(400).json({ message: resultado.error });
        }
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST SEARCH: Búsqueda avanzada de máquinas
router.post('/search', verifyToken, sanitizarBusqueda, limitarResultados, async (req, res) => {
    try {
        const resultado = await MaquinaService.buscarMaquinas(req.body);
        
        if (resultado.success) {
            res.status(200).json(resultado.data);
        } else {
            res.status(500).json({ message: resultado.error });
        }
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET HISTORY: Obtener historial de estados de una máquina
router.post('/getHistory', verifyToken, validarObtenerHistorial, async (req, res) => {
    try {
        const { numeroSerie } = req.body;

        const maquina = await modelsMaquina.findOne({ numeroSerie }).select('historialEstados numeroSerie tipo marca modelo');

        if (!maquina) {
            return res.status(404).json({ message: 'Máquina no encontrada' });
        }

        res.status(200).json({
            numeroSerie: maquina.numeroSerie,
            tipo: maquina.tipo,
            marca: maquina.marca,
            modelo: maquina.modelo,
            historial: maquina.historialEstados.sort((a, b) => b.fechaCambio - a.fechaCambio)
        });
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;
