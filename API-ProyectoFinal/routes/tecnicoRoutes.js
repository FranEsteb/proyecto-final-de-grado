const express = require('express');
const modelsTecnico = require('../models/modelsTecnico');
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// GET ALL: Obtener todos los técnicos
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const tecnicos = await modelsTecnico.find().sort({ nombre: 1 });
        res.status(200).json(tecnicos);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET ACTIVOS: Obtener solo técnicos activos
router.get('/getActivos', verifyToken, async (req, res) => {
    try {
        const tecnicos = await modelsTecnico.find({ activo: true }).sort({ nombre: 1 });
        res.status(200).json(tecnicos);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET DISPONIBLES: Obtener técnicos activos y disponibles
router.get('/getDisponibles', verifyToken, async (req, res) => {
    try {
        const tecnicos = await modelsTecnico.find({
            activo: true,
            disponible: true
        }).sort({ nombre: 1 });
        res.status(200).json(tecnicos);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET BY ID: Obtener técnico por ID
router.get('/:id', verifyToken, async (req, res) => {
    try {
        const tecnico = await modelsTecnico.findById(req.params.id);
        if (!tecnico) {
            return res.status(404).json({ message: "Técnico no encontrado" });
        }
        res.status(200).json(tecnico);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST NEW: Crear nuevo técnico
router.post('/new', verifyToken, async (req, res) => {
    try {
        const nuevoTecnico = new modelsTecnico({
            nombre: req.body.nombre,
            apellidos: req.body.apellidos,
            email: req.body.email,
            telefono: req.body.telefono,
            especialidad: req.body.especialidad || 'General',
            certificaciones: req.body.certificaciones || [],
            activo: req.body.activo !== undefined ? req.body.activo : true,
            disponible: req.body.disponible !== undefined ? req.body.disponible : true,
            fechaContratacion: req.body.fechaContratacion || new Date(),
            tarifaHora: req.body.tarifaHora,
            notas: req.body.notas
        });

        const savedTecnico = await nuevoTecnico.save();
        res.status(201).json(savedTecnico);
    } catch (error) {
        if (error.code === 11000) {
            return res.status(400).json({ message: "Ya existe un técnico con ese email" });
        }
        res.status(400).json({ message: error.message });
    }
});

// PATCH UPDATE: Actualizar técnico
router.patch('/update/:id', verifyToken, async (req, res) => {
    try {
        const updateData = {
            nombre: req.body.nombre,
            apellidos: req.body.apellidos,
            email: req.body.email,
            telefono: req.body.telefono,
            especialidad: req.body.especialidad,
            certificaciones: req.body.certificaciones,
            activo: req.body.activo,
            disponible: req.body.disponible,
            tarifaHora: req.body.tarifaHora,
            notas: req.body.notas
        };

        // Eliminar campos undefined
        Object.keys(updateData).forEach(key =>
            updateData[key] === undefined && delete updateData[key]
        );

        const tecnico = await modelsTecnico.findByIdAndUpdate(
            req.params.id,
            { $set: updateData },
            { new: true, runValidators: true }
        );

        if (!tecnico) {
            return res.status(404).json({ message: "Técnico no encontrado" });
        }

        res.status(200).json(tecnico);
    } catch (error) {
        if (error.code === 11000) {
            return res.status(400).json({ message: "Ya existe un técnico con ese email" });
        }
        res.status(400).json({ message: error.message });
    }
});

// PATCH TOGGLE DISPONIBILIDAD: Cambiar disponibilidad del técnico
router.patch('/toggleDisponibilidad/:id', verifyToken, async (req, res) => {
    try {
        const tecnico = await modelsTecnico.findById(req.params.id);
        if (!tecnico) {
            return res.status(404).json({ message: "Técnico no encontrado" });
        }

        tecnico.disponible = !tecnico.disponible;
        await tecnico.save();

        res.status(200).json({
            message: `Técnico marcado como ${tecnico.disponible ? 'disponible' : 'no disponible'}`,
            tecnico
        });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// PATCH ACTUALIZAR ESTADISTICAS: Incrementar reparaciones completadas
router.patch('/completarReparacion/:id', verifyToken, async (req, res) => {
    try {
        const tecnico = await modelsTecnico.findByIdAndUpdate(
            req.params.id,
            { $inc: { reparacionesCompletadas: 1 } },
            { new: true }
        );

        if (!tecnico) {
            return res.status(404).json({ message: "Técnico no encontrado" });
        }

        res.status(200).json(tecnico);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// PATCH ACTUALIZAR CALIFICACION: Actualizar calificación promedio
router.patch('/actualizarCalificacion/:id', verifyToken, async (req, res) => {
    try {
        const { calificacion } = req.body;

        if (calificacion < 0 || calificacion > 5) {
            return res.status(400).json({ message: "La calificación debe estar entre 0 y 5" });
        }

        const tecnico = await modelsTecnico.findById(req.params.id);
        if (!tecnico) {
            return res.status(404).json({ message: "Técnico no encontrado" });
        }

        // Calcular nuevo promedio
        const totalReparaciones = tecnico.reparacionesCompletadas;
        const promedioActual = tecnico.calificacionPromedio;
        const nuevoPromedio = ((promedioActual * totalReparaciones) + calificacion) / (totalReparaciones + 1);

        tecnico.calificacionPromedio = Math.round(nuevoPromedio * 10) / 10; // Redondear a 1 decimal
        await tecnico.save();

        res.status(200).json(tecnico);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE: Desactivar técnico (soft delete)
router.delete('/desactivar/:id', verifyToken, async (req, res) => {
    try {
        const tecnico = await modelsTecnico.findByIdAndUpdate(
            req.params.id,
            { $set: { activo: false, disponible: false } },
            { new: true }
        );

        if (!tecnico) {
            return res.status(404).json({ message: "Técnico no encontrado" });
        }

        res.status(200).json({ message: "Técnico desactivado correctamente", tecnico });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE PERMANENTE: Eliminar técnico permanentemente (usar con cuidado)
router.delete('/eliminar/:id', verifyToken, async (req, res) => {
    try {
        const tecnico = await modelsTecnico.findByIdAndDelete(req.params.id);

        if (!tecnico) {
            return res.status(404).json({ message: "Técnico no encontrado" });
        }

        res.status(200).json({ message: "Técnico eliminado permanentemente" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// GET ESTADISTICAS: Obtener estadísticas generales de técnicos
router.get('/stats/general', verifyToken, async (req, res) => {
    try {
        const total = await modelsTecnico.countDocuments();
        const activos = await modelsTecnico.countDocuments({ activo: true });
        const disponibles = await modelsTecnico.countDocuments({ activo: true, disponible: true });

        const estadisticas = {
            total,
            activos,
            disponibles,
            noDisponibles: activos - disponibles,
            inactivos: total - activos
        };

        res.status(200).json(estadisticas);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;
