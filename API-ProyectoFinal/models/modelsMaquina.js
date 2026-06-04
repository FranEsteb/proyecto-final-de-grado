const mongoose = require('mongoose');

const maquinaSchema = new mongoose.Schema({
    numeroSerie: { type: String, required: true, unique: true },
    tipo: { type: String, required: true },                     
    modelo: { type: String },
    marca: { type: String },
    fechaCompra: { type: Date },
    estado: { type: String, enum: ['operativa', 'en reparación', 'fuera de servicio', 'mantenimiento'], default: 'operativa' },
    proveedor: { type: mongoose.Schema.Types.ObjectId, ref: 'modelsProveedor' },
    ubicacion: { type: String },
    mantenimientoProgramado: { type: Date },
    imagenMaquina: { type: String },
    horasUso: { type: Number, default: 0 },
    ultimoMantenimiento: { type: Date },
    costoCompra: { type: Number },
    costoReparacion: { type: Number, default: 0 },
    costoMantenimiento: { type: Number, default: 0 },
    garantia: {
        fechaInicio: { type: Date },
        fechaFin: { type: Date },
        proveedor: { type: String }
    },
    especificaciones: {
        peso: { type: Number },
        dimensiones: { type: String },
        consumoEnergia: { type: Number },
        capacidadMaxima: { type: Number }
    },
    historialEstados: [{
        estado: { type: String },
        fechaCambio: { type: Date, default: Date.now },
        motivo: { type: String },
        usuario: { type: String }
    }],
    notificaciones: {
        mantenimientoPendiente: { type: Boolean, default: false },
        garantiaPorVencer: { type: Boolean, default: false },
        reparacionNecesaria: { type: Boolean, default: false }
    }
}, {
    timestamps: true
});

module.exports = mongoose.model('modelsMaquina', maquinaSchema, "maquinas");
