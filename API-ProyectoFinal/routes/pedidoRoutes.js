const express = require('express');
const modelsPedido = require('../models/modelsPedido');
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// GET ALL: Obtener todos los pedidos (WPF)
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const pedidos = await modelsPedido.find().sort({ fechaPedido: -1 });
        res.status(200).json(pedidos);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST GET BY PROVEEDOR: Obtener pedidos de un proveedor específico (WPF)
router.post('/getByProveedor', verifyToken, async (req, res) => {
    try {
        const { cif } = req.body;
        if (!cif) return res.status(400).json({ message: "Falta el CIF del proveedor" });

        const pedidos = await modelsPedido.find({ proveedorCif: cif }).sort({ fechaPedido: -1 });
        res.status(200).json(pedidos);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST NEW: Crear nuevo pedido (WPF)
router.post('/new', verifyToken, async (req, res) => {
    try {
        const nuevoPedido = new modelsPedido({
            numeroPedido: req.body.numeroPedido,
            proveedorCif: req.body.proveedorCif,
            proveedorNombre: req.body.proveedorNombre,
            productos: req.body.productos,
            estado: req.body.estado || 'pendiente',
            fechaPedido: req.body.fechaPedido || new Date(),
            fechaEntregaEsperada: req.body.fechaEntregaEsperada,
            observaciones: req.body.observaciones
        });

        const savedPedido = await nuevoPedido.save();
        res.status(200).json(savedPedido);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// PATCH UPDATE: Actualizar pedido por número de pedido (WPF)
router.patch('/update/:numeroPedido', verifyToken, async (req, res) => {
    try {
        const { numeroPedido } = req.params;
        if (!numeroPedido) return res.status(400).json({ message: "Falta el número de pedido" });

        const updateFields = {};

        if (req.body.estado !== undefined) updateFields.estado = req.body.estado;
        if (req.body.fechaEntrega !== undefined) updateFields.fechaEntrega = req.body.fechaEntrega;
        if (req.body.observaciones !== undefined) updateFields.observaciones = req.body.observaciones;
        updateFields.updatedAt = new Date();

        const result = await modelsPedido.updateOne({ numeroPedido }, { $set: updateFields });
        if (result.modifiedCount === 0) return res.status(404).json({ message: "Pedido no encontrado o datos sin cambios" });

        res.status(200).json({ message: "Pedido actualizado" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE: Eliminar pedido por número de pedido (WPF)
router.delete('/delete', verifyToken, async (req, res) => {
    try {
        const { numeroPedido } = req.body;
        if (!numeroPedido) return res.status(400).json({ message: "Falta el número de pedido" });

        const result = await modelsPedido.deleteOne({ numeroPedido });
        if (result.deletedCount === 0) return res.status(404).json({ message: "Pedido no encontrado" });

        res.status(200).json({ message: `Pedido ${numeroPedido} eliminado` });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

module.exports = router;
