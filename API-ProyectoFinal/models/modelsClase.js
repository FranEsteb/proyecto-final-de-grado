const mongoose = require('mongoose');

const claseSchema = new mongoose.Schema({
    idClase: { type: String, required: true, unique: true },          // ID único manual
    nombre: { type: String, required: true },
    descripcion: { type: String },
    instructor: { type: String },
    fechaHora: { type: Date, required: true },
    duracionMinutos: { type: Number, required: true },
    capacidadMaxima: { type: Number, required: true },
    inscritos: [{ type: mongoose.Schema.Types.ObjectId, ref: 'modelsUsuario' }],
    sala: { type: String },
    estado: { type: String, enum: ['disponible', 'completa', 'cancelada'], default: 'disponible' }
}, {
    timestamps: true
});

module.exports = mongoose.model('modelsClase', claseSchema, "clases");
