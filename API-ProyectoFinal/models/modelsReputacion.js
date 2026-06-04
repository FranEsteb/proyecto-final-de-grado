const mongoose = require('mongoose');

const reputacionSchema = new mongoose.Schema({
    idRep: { type: String, required: true, unique: true },   // ID personalizado
    usuario: { type: mongoose.Schema.Types.ObjectId, ref: 'modelsUsuario', required: true },
    tipo: { type: String, enum: ['recompensa', 'sancion'], required: true },
    motivo: { type: String, required: true },
    puntos: { type: Number, required: true },
    fecha: { type: Date, default: Date.now }
}, {
    timestamps: true
});

module.exports = mongoose.model('modelsReputacion', reputacionSchema, "reputaciones");
