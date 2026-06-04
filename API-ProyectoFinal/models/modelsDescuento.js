const mongoose = require('mongoose');

const descuentoSchema = new mongoose.Schema({
    descuentoId: { type: String, required: true, unique: true },        // ID único manual
    usuario: { type: mongoose.Schema.Types.ObjectId, ref: 'modelsUsuario', required: true },
    tipo: { type: String, required: true },
    porcentaje: { type: Number, required: true },
    fechaInicio: { type: Date, required: true },
    fechaFin: { type: Date, required: true },
    motivo: { type: String }
}, {
    timestamps: true
});

module.exports = mongoose.model('modelsDescuento', descuentoSchema, "descuentos");
