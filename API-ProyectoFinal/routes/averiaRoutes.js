const express = require('express');
const modelsAveria = require('../models/modelsAveria');
const modelsCosto = require('../models/modelsCosto');
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// GET ALL: Obtener todas las averías
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const averias = await modelsAveria.find();
        res.status(200).json(averias);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST NEW: Registrar nueva avería
router.post('/new', verifyToken, async (req, res) => {
    try {
        const nuevaAveria = new modelsAveria({
            maquina: req.body.ElementoAfectado || req.body.numeroSerie,
            descripcion: req.body.Descripcion || req.body.descripcion,
            prioridad: req.body.Prioridad || req.body.prioridad,
            estado: req.body.Estado || req.body.estado || 'pendiente',
            fechaResolucion: req.body.FechaResolucion || req.body.fechaResolucion,
            observaciones: req.body.Observaciones || req.body.observaciones,
            costoReparacion: req.body.CostoReparacion || req.body.costoReparacion,
            tecnicoAsignado: req.body.TecnicoAsignado || req.body.tecnicoAsignado,
            fechaProgramada: req.body.FechaProgramada || req.body.fechaProgramada,
            estadoReparacion: req.body.EstadoReparacion || req.body.estadoReparacion || 'programada'
        });

        const savedAveria = await nuevaAveria.save();

        // Si la avería tiene costo de reparación, crear automáticamente un registro de costo
        if (savedAveria.costoReparacion && savedAveria.costoReparacion > 0) {
            try {
                const nuevoCosto = new modelsCosto({
                    tipoCosto: 'Reparacion',
                    monto: savedAveria.costoReparacion,
                    fecha: new Date(),
                    descripcion: `Reparación: ${savedAveria.descripcion}`,
                    averia: savedAveria._id,
                    observaciones: `Costo estimado de avería - Elemento: ${savedAveria.maquina}`,
                    usuarioRegistro: req.user?.email || 'Sistema',
                    estadoPago: 'Pendiente'
                });

                await nuevoCosto.save();
                console.log('✅ Registro de costo creado automáticamente para avería:', savedAveria._id);
            } catch (costoError) {
                console.error('⚠️ Error al crear registro de costo automático:', costoError.message);
                // No fallar la creación de la avería si hay error en el costo
            }
        }

        res.status(200).json(savedAveria);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// POST GET ONE: Obtener primera avería de una máquina
router.post('/getOne', verifyToken, async (req, res) => {
    try {
        const averia = await modelsAveria.findOne({ maquina: req.body.numeroSerie });
        if (!averia) return res.status(404).json({ message: "Avería no encontrada para esa máquina" });
        res.status(200).json(averia);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST GET BY numeroSerie: Obtener todas las averías de una máquina
router.post('/getByNumeroSerie', verifyToken, async (req, res) => {
    try {
        const averias = await modelsAveria.find({ maquina: req.body.numeroSerie });
        if (averias.length === 0) return res.status(404).json({ message: "No se encontraron averías para esa máquina" });
        res.status(200).json(averias);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// PATCH UPDATE: Modificar avería por ID
router.patch('/update', verifyToken, async (req, res) => {
    try {
        const updateData = {
            maquina: req.body.ElementoAfectado || req.body.maquina,
            descripcion: req.body.Descripcion || req.body.descripcion,
            prioridad: req.body.Prioridad || req.body.prioridad,
            estado: req.body.Estado || req.body.estado,
            fechaResolucion: req.body.FechaResolucion || req.body.fechaResolucion,
            observaciones: req.body.Observaciones || req.body.observaciones,
            tecnicoAsignado: req.body.TecnicoAsignado || req.body.tecnicoAsignado,
            fechaProgramada: req.body.FechaProgramada || req.body.fechaProgramada,
            estadoReparacion: req.body.EstadoReparacion || req.body.estadoReparacion,
            costoReparacion: req.body.CostoReparacion || req.body.costoReparacion,
            updatedAt: new Date()
        };

        // Eliminar campos undefined
        Object.keys(updateData).forEach(key => updateData[key] === undefined && delete updateData[key]);

        const result = await modelsAveria.updateOne(
            { _id: req.body.Id || req.body._id },
            { $set: updateData }
        );

        if (result.modifiedCount === 0) return res.status(404).json({ message: "Avería no encontrada o sin cambios" });

        res.status(200).json({ message: "Avería actualizada correctamente" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE: Eliminar avería de una máquina (por numeroSerie + descripcion)
router.delete('/delete', verifyToken, async (req, res) => {
    try {
        const result = await modelsAveria.deleteOne({
            maquina: req.body.numeroSerie,
            descripcion: req.body.descripcion
        });

        if (result.deletedCount === 0) return res.status(404).json({ message: "Avería no encontrada" });

        res.status(200).json({ message: "Avería eliminada correctamente" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

module.exports = router;
