const mongoose = require('mongoose');

// Schema para líneas de pedido (productos con cantidad)
const lineaPedidoSchema = new mongoose.Schema({
    producto: {
        type: String,
        required: true
    },
    cantidad: {
        type: Number,
        required: true,
        min: 1
    },
    precioUnitario: {
        type: Number,
        default: 0
    }
}, { _id: false });

// Schema principal para Pedidos
const pedidoSchema = new mongoose.Schema({
    numeroPedido: {
        type: String,
        required: true,
        unique: true,
        index: true
    },
    proveedorCif: {
        type: String,
        required: true,
        index: true
    },
    proveedorNombre: {
        type: String,
        required: true
    },
    productos: [lineaPedidoSchema],
    estado: {
        type: String,
        enum: ['pendiente', 'confirmado', 'entregado', 'cancelado'],
        default: 'pendiente',
        index: true
    },
    fechaPedido: {
        type: Date,
        default: Date.now
    },
    fechaEntregaEsperada: {
        type: Date
    },
    fechaEntrega: {
        type: Date
    },
    observaciones: {
        type: String,
        maxlength: 500
    },
    createdAt: {
        type: Date,
        default: Date.now
    },
    updatedAt: {
        type: Date,
        default: Date.now
    }
});

// Índice compuesto para búsquedas frecuentes
pedidoSchema.index({ proveedorCif: 1, estado: 1 });
pedidoSchema.index({ fechaPedido: -1 });

module.exports = mongoose.model('Pedido', pedidoSchema);
