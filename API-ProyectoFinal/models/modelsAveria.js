const mongoose = require('mongoose');

const averiaSchema = new mongoose.Schema({
    maquina: { type: String, required: true }, 
    descripcion: { type: String, required: true },
    fechaReporte: { type: Date, default: Date.now },
    prioridad: { type: String, enum: ['baja', 'media', 'alta'], default: 'media' },
    estado: { type: String, enum: ['pendiente', 'en proceso', 'resuelta'], default: 'pendiente' },
    fechaResolucion: { type: Date },
    observaciones: { type: String },
    costoReparacion: { type: Number },
    tecnicoAsignado: { type: String },
    fechaProgramada: { type: Date },
    estadoReparacion: { type: String, enum: ['programada', 'en_progreso', 'completada', 'cancelada'], default: 'programada' }
}, {
    timestamps: true
});

module.exports = mongoose.model('modelsAveria', averiaSchema, "averias");
