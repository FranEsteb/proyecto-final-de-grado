const mongoose = require('mongoose');

// Esquema para el control de costos de reparación y mantenimiento
const costoSchema = new mongoose.Schema({
    // Tipo de costo
    tipoCosto: {
        type: String,
        enum: ['Reparacion', 'Mantenimiento', 'Repuesto', 'ManoDeObra', 'Otro'],
        required: true
    },

    // Información del costo
    monto: { type: Number, required: true, min: 0 },
    fecha: { type: Date, required: true, default: Date.now },
    descripcion: { type: String, required: true },

    // Relaciones
    maquina: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'modelsMaquina'
    },
    averia: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'modelsAveria'
    },
    tecnico: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'modelsTecnico'
    },
    proveedor: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'modelsProveedor'
    },

    // Información adicional
    numeroFactura: { type: String },
    observaciones: { type: String },

    // Detalles de repuestos (si aplica)
    repuestos: [{
        nombre: { type: String },
        cantidad: { type: Number },
        precioUnitario: { type: Number },
        subtotal: { type: Number }
    }],

    // Auditoría
    usuarioRegistro: { type: String }, // Email o nombre del usuario que registró el costo

    // Estado del pago
    estadoPago: {
        type: String,
        enum: ['Pendiente', 'Pagado', 'Parcial'],
        default: 'Pendiente'
    },
    fechaPago: { type: Date },

    // Aprobación (para costos mayores)
    requiereAprobacion: { type: Boolean, default: false },
    aprobado: { type: Boolean, default: false },
    aprobadoPor: { type: String },
    fechaAprobacion: { type: Date }
}, {
    timestamps: true
});

// Índices para mejorar el rendimiento de las consultas
costoSchema.index({ fecha: -1 });
costoSchema.index({ tipoCosto: 1 });
costoSchema.index({ maquina: 1 });
costoSchema.index({ tecnico: 1 });
costoSchema.index({ estadoPago: 1 });

// Middleware pre-save: marcar costos mayores a 1000 para aprobación
costoSchema.pre('save', function(next) {
    if (this.isNew && this.monto > 1000) {
        this.requiereAprobacion = true;
    }
    next();
});

// Método estático para obtener estadísticas de costos
costoSchema.statics.obtenerEstadisticas = async function(filtros = {}) {
    const match = {};

    // Aplicar filtros de fecha
    if (filtros.fechaDesde || filtros.fechaHasta) {
        match.fecha = {};
        if (filtros.fechaDesde) match.fecha.$gte = new Date(filtros.fechaDesde);
        if (filtros.fechaHasta) match.fecha.$lte = new Date(filtros.fechaHasta);
    }

    // Filtro por tipo
    if (filtros.tipoCosto) {
        match.tipoCosto = filtros.tipoCosto;
    }

    // Filtro por máquina
    if (filtros.maquina) {
        match.maquina = mongoose.Types.ObjectId(filtros.maquina);
    }

    const estadisticas = await this.aggregate([
        { $match: match },
        {
            $group: {
                _id: '$tipoCosto',
                total: { $sum: '$monto' },
                cantidad: { $sum: 1 },
                promedio: { $avg: '$monto' }
            }
        }
    ]);

    // Calcular total general
    const totalGeneral = await this.aggregate([
        { $match: match },
        {
            $group: {
                _id: null,
                total: { $sum: '$monto' },
                cantidad: { $sum: 1 }
            }
        }
    ]);

    return {
        porTipo: estadisticas,
        total: totalGeneral[0] || { total: 0, cantidad: 0 }
    };
};

// Método estático para obtener costos por máquina
costoSchema.statics.costosPorMaquina = async function(maquinaId, fechaDesde, fechaHasta) {
    const match = { maquina: mongoose.Types.ObjectId(maquinaId) };

    if (fechaDesde || fechaHasta) {
        match.fecha = {};
        if (fechaDesde) match.fecha.$gte = new Date(fechaDesde);
        if (fechaHasta) match.fecha.$lte = new Date(fechaHasta);
    }

    const costos = await this.find(match)
        .populate('tecnico', 'nombre apellidos')
        .populate('proveedor', 'nombre')
        .sort({ fecha: -1 });

    const total = costos.reduce((sum, costo) => sum + costo.monto, 0);

    return {
        costos,
        total,
        cantidad: costos.length
    };
};

module.exports = mongoose.model('modelsCosto', costoSchema, "costos");
