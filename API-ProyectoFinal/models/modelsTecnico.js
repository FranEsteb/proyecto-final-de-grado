const mongoose = require('mongoose');

// Esquema para los técnicos que realizan mantenimiento y reparaciones
const tecnicoSchema = new mongoose.Schema({
    nombre: { type: String, required: true },
    apellidos: { type: String, required: true },
    email: { type: String, required: true, unique: true },
    telefono: { type: String, required: true },

    // Información profesional
    especialidad: {
        type: String,
        enum: ['Electromecanica', 'Hidraulica', 'Electronica', 'General'],
        default: 'General'
    },
    certificaciones: [{ type: String }],

    // Estado del técnico
    activo: { type: Boolean, default: true },
    disponible: { type: Boolean, default: true },

    // Información adicional
    fechaContratacion: { type: Date, default: Date.now },
    tarifaHora: { type: Number },

    // Estadísticas
    reparacionesCompletadas: { type: Number, default: 0 },
    calificacionPromedio: { type: Number, default: 0, min: 0, max: 5 },

    // Observaciones
    notas: { type: String }
}, {
    timestamps: true
});

// Método virtual para obtener el nombre completo
tecnicoSchema.virtual('nombreCompleto').get(function() {
    return `${this.nombre} ${this.apellidos}`;
});

// Asegurar que los virtuals se incluyan al convertir a JSON
tecnicoSchema.set('toJSON', { virtuals: true });
tecnicoSchema.set('toObject', { virtuals: true });

module.exports = mongoose.model('modelsTecnico', tecnicoSchema, "tecnicos");
