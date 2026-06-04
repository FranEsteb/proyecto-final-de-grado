const express = require('express');
const modelsDescuento = require('../models/modelsDescuento');
const modelsUsuario = require('../models/modelsUsuario');
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// GET ALL
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const descuentos = await modelsDescuento.find().populate('usuario');
        res.status(200).json(descuentos);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// CREATE NEW (descuentoId obligatorio en body)
router.post('/new', verifyToken, async (req, res) => {
    try {
        const usuario = await modelsUsuario.findOne({ dni: req.body.usuarioDni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado con ese DNI" });

        const nuevoDescuento = new modelsDescuento({
            descuentoId: req.body.descuentoId,   // lo proporciona el cliente
            usuario: usuario._id,
            tipo: req.body.tipo,
            porcentaje: req.body.porcentaje,
            fechaInicio: req.body.fechaInicio,
            fechaFin: req.body.fechaFin,
            motivo: req.body.motivo
        });

        const savedDescuento = await nuevoDescuento.save();
        res.status(200).json(savedDescuento);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// UPDATE by descuentoId
router.patch('/update', verifyToken, async (req, res) => {
    try {
        const { descuentoId } = req.body;
        if (!descuentoId) return res.status(400).json({ message: "Falta el campo descuentoId" });

        const updateFields = {};

        if (req.body.usuarioDni !== undefined) {
            const usuario = await modelsUsuario.findOne({ dni: req.body.usuarioDni });
            if (!usuario) return res.status(404).json({ message: "Usuario no encontrado con ese DNI" });
            updateFields.usuario = usuario._id;
        }

        if (req.body.tipo !== undefined) updateFields.tipo = req.body.tipo;
        if (req.body.porcentaje !== undefined) updateFields.porcentaje = req.body.porcentaje;
        if (req.body.fechaInicio !== undefined) updateFields.fechaInicio = req.body.fechaInicio;
        if (req.body.fechaFin !== undefined) updateFields.fechaFin = req.body.fechaFin;
        if (req.body.motivo !== undefined) updateFields.motivo = req.body.motivo;

        const result = await modelsDescuento.updateOne({ descuentoId }, { $set: updateFields });
        if (result.modifiedCount === 0) return res.status(404).json({ message: "Descuento no encontrado o datos sin cambios" });

        res.status(200).json({ message: "Descuento actualizado" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE by descuentoId
router.delete('/delete', verifyToken, async (req, res) => {
    try {
        const { descuentoId } = req.body;
        const result = await modelsDescuento.deleteOne({ descuentoId });
        if (result.deletedCount === 0) return res.status(404).json({ message: "Descuento no encontrado" });

        res.status(200).json({ message: `Descuento con ID ${descuentoId} eliminado` });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// GET ONE by descuentoId
router.post('/getOne', verifyToken, async (req, res) => {
    try {
        if (!req.body.descuentoId) return res.status(400).json({ message: "Falta el campo descuentoId" });

        const descuento = await modelsDescuento.findOne({ descuentoId: req.body.descuentoId }).populate('usuario');
        if (!descuento) return res.status(404).json({ message: "Descuento no encontrado" });

        res.status(200).json(descuento);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;
