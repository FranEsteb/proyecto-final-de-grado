const mongoose = require('mongoose');

const membresiaSchema = new mongoose.Schema({
    usuario: { type: mongoose.Schema.Types.ObjectId, ref: 'modelsUsuario', required: true },
    tipo: { 
        type: String, 
        enum: ['basica', 'premium', 'vip'], 
        default: 'basica',
        required: true 
    },
    fechaInicio: { type: Date, required: true, default: Date.now },
    fechaFin: { type: Date, required: true },
    estado: { 
        type: String, 
        enum: ['activa', 'pausada', 'expirada', 'cancelada'], 
        default: 'activa',
        required: true 
    },
    descuentoActual: { type: Number, default: 0 },
    beneficios: {
        descuentoPorPermanencia: { type: Boolean, default: true },
        descuentoMaximo: { type: Number, default: 20 },
        accesoEspecial: { type: Boolean, default: false }
    },
    historialDescuentos: [{
        porcentaje: { type: Number, required: true },
        fechaAplicacion: { type: Date, default: Date.now },
        motivo: { type: String, required: true },
        aplicadoPor: { type: mongoose.Schema.Types.ObjectId, ref: 'modelsUsuario' }
    }],
    notificaciones: {
        recordatorioVencimiento: { type: Boolean, default: true },
        diasAnticipacion: { type: Number, default: 7 }
    }
}, {
    timestamps: true
});

membresiaSchema.methods.calcularDescuentoPorPermanencia = function() {
    const ahora = new Date();
    const mesesActivos = Math.floor((ahora - this.fechaInicio) / (1000 * 60 * 60 * 24 * 30));

    // Calcular 2% por cada año de suscripción (máximo 10%)
    const anosActivos = Math.floor(mesesActivos / 12);
    const descuentoAutomatico = Math.min(anosActivos * 2, 10);

    return descuentoAutomatico;
};

membresiaSchema.methods.obtenerTiempoRestante = function() {
    const ahora = new Date();
    const tiempoRestante = this.fechaFin - ahora;
    const diasRestantes = Math.ceil(tiempoRestante / (1000 * 60 * 60 * 24));
    
    if (diasRestantes <= 0) {
        return { diasRestantes: 0, estado: 'expirada' };
    }
    
    return { 
        diasRestantes, 
        estado: this.estado,
        meses: Math.floor(diasRestantes / 30),
        dias: diasRestantes % 30
    };
};

membresiaSchema.methods.estaProximaAVencer = function(diasAnticipacion = 7) {
    const tiempoRestante = this.obtenerTiempoRestante();
    return tiempoRestante.diasRestantes <= diasAnticipacion && tiempoRestante.diasRestantes > 0;
};

membresiaSchema.statics.obtenerMembresiaActiva = async function(usuarioId) {
    return await this.findOne({
        usuario: usuarioId,
        estado: 'activa',
        fechaFin: { $gt: new Date() }
    }).populate('usuario', 'nombre apellidos email');
};

module.exports = mongoose.model('modelsMembresia', membresiaSchema, 'membresias');