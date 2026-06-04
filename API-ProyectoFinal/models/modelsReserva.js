const mongoose = require('mongoose');

const reservaSchema = new mongoose.Schema({
    idReserva: { type: String, required: true, unique: true },           // NUEVO ID ÚNICO
    usuario: { type: mongoose.Schema.Types.ObjectId, ref: 'modelsUsuario', required: true },
    clase: { type: mongoose.Schema.Types.ObjectId, ref: 'modelsClase', required: true },
    fechaReserva: { type: Date, default: Date.now },
    estado: { type: String, enum: ['activa', 'cancelada', 'asistida', 'no asistida'], default: 'activa' },
    observaciones: { type: String }
}, {
    timestamps: true
});

module.exports = mongoose.model('modelsReserva', reservaSchema, "reservas");
