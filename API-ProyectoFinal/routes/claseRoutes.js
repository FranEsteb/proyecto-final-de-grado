const express = require('express');
const modelsClase = require('../models/modelsClase');
const modelsUsuario = require('../models/modelsUsuario');
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// GET ALL
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const clases = await modelsClase.find().populate('inscritos');
        res.status(200).json(clases);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// CREATE NEW
router.post('/new', verifyToken, async (req, res) => {
    try {
        const nuevaClase = new modelsClase({
            idClase: req.body.idClase, // <-- NUEVO
            nombre: req.body.nombre,
            descripcion: req.body.descripcion,
            instructor: req.body.instructor,
            fechaHora: req.body.fechaHora,
            duracionMinutos: req.body.duracionMinutos,
            capacidadMaxima: req.body.capacidadMaxima,
            sala: req.body.sala,
            estado: req.body.estado
        });

        const savedClase = await nuevaClase.save();
        res.status(200).json(savedClase);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// UPDATE by idClase
router.patch('/update', verifyToken, async (req, res) => {
    try {
        const { idClase } = req.body;
        if (!idClase) return res.status(400).json({ message: "Falta el campo idClase" });

        const updateFields = {};
        if (req.body.nombre !== undefined) updateFields.nombre = req.body.nombre;
        if (req.body.descripcion !== undefined) updateFields.descripcion = req.body.descripcion;
        if (req.body.instructor !== undefined) updateFields.instructor = req.body.instructor;
        if (req.body.fechaHora !== undefined) updateFields.fechaHora = req.body.fechaHora;
        if (req.body.duracionMinutos !== undefined) updateFields.duracionMinutos = req.body.duracionMinutos;
        if (req.body.capacidadMaxima !== undefined) updateFields.capacidadMaxima = req.body.capacidadMaxima;

        // Convertir DNIs a ObjectIds para el campo inscritos
        if (req.body.inscritos !== undefined) {
            const inscritosObjectIds = [];
            for (const dni of req.body.inscritos) {
                const usuario = await modelsUsuario.findOne({ dni: dni });
                if (usuario) {
                    inscritosObjectIds.push(usuario._id);
                }
            }
            updateFields.inscritos = inscritosObjectIds;
        }

        if (req.body.sala !== undefined) updateFields.sala = req.body.sala;
        if (req.body.estado !== undefined) updateFields.estado = req.body.estado;

        const result = await modelsClase.updateOne({ idClase }, { $set: updateFields });
        if (result.modifiedCount === 0) return res.status(404).json({ message: "Clase no encontrada o datos sin cambios" });

        res.status(200).json({ message: "Clase actualizada" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE by idClase
router.delete('/delete', verifyToken, async (req, res) => {
    try {
        const { idClase } = req.body;
        const result = await modelsClase.deleteOne({ idClase });
        if (result.deletedCount === 0) return res.status(404).json({ message: "Clase no encontrada" });

        res.status(200).json({ message: `Clase con ID ${idClase} eliminada` });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// GET ONE by idClase
router.post('/getOne', verifyToken, async (req, res) => {
    try {
        if (!req.body.idClase) return res.status(400).json({ message: "Falta el campo idClase" });

        const clase = await modelsClase.findOne({ idClase: req.body.idClase }).populate('inscritos');
        if (!clase) return res.status(404).json({ message: "Clase no encontrada" });

        res.status(200).json(clase);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;
