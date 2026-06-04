const express = require('express');
const modelsProveedor = require('../models/modelsProveedor');
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// GET ALL: Obtener todos los proveedores (WPF)
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const proveedores = await modelsProveedor.find();
        res.status(200).json(proveedores);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST NEW: Crear nuevo proveedor (WPF)
router.post('/new', verifyToken, async (req, res) => {
    try {
        const nuevoProveedor = new modelsProveedor({
            nombre: req.body.nombre,
            cif: req.body.cif,
            direccion: req.body.direccion,
            telefono: req.body.telefono,
            email: req.body.email,
            productosSuministrados: req.body.productosSuministrados,
            observaciones: req.body.observaciones
        });

        const savedProveedor = await nuevoProveedor.save();
        res.status(200).json(savedProveedor);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// PATCH UPDATE: Actualizar proveedor por cif (WPF)
router.patch('/update', verifyToken, async (req, res) => {
    try {
        const { cif } = req.body;
        if (!cif) return res.status(400).json({ message: "Falta el campo cif" });

        const updateFields = {};

        if (req.body.nombre !== undefined) updateFields.nombre = req.body.nombre;
        if (req.body.direccion !== undefined) updateFields.direccion = req.body.direccion;
        if (req.body.telefono !== undefined) updateFields.telefono = req.body.telefono;
        if (req.body.email !== undefined) updateFields.email = req.body.email;
        if (req.body.productosSuministrados !== undefined) updateFields.productosSuministrados = req.body.productosSuministrados;
        if (req.body.observaciones !== undefined) updateFields.observaciones = req.body.observaciones;

        const result = await modelsProveedor.updateOne({ cif }, { $set: updateFields });
        if (result.modifiedCount === 0) return res.status(404).json({ message: "Proveedor no encontrado o datos sin cambios" });

        res.status(200).json({ message: "Proveedor actualizado" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE: Eliminar proveedor por cif (WPF)
router.delete('/delete', verifyToken, async (req, res) => {
    try {
        const { cif } = req.body;
        const result = await modelsProveedor.deleteOne({ cif });
        if (result.deletedCount === 0) return res.status(404).json({ message: "Proveedor no encontrado" });

        res.status(200).json({ message: `Proveedor con CIF ${cif} eliminado` });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// GET ONE: Obtener proveedor por cif (WPF)
router.post('/getOne', verifyToken, async (req, res) => {
    try {
        if (!req.body.cif) return res.status(400).json({ message: "Falta el campo cif" });

        const proveedor = await modelsProveedor.findOne({ cif: req.body.cif });
        if (!proveedor) return res.status(404).json({ message: "Proveedor no encontrado" });

        res.status(200).json(proveedor);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;
