const modelsMaquina = require('../models/modelsMaquina');

class MaquinaService {
    static async registrarMaquina(datosBasicos, usuario) {
        try {
            const maquina = new modelsMaquina({
                ...datosBasicos,
                historialEstados: [{
                    estado: datosBasicos.estado || 'operativa',
                    fechaCambio: new Date(),
                    motivo: 'Registro inicial',
                    usuario: usuario
                }]
            });
            
            await maquina.save();
            return { success: true, data: maquina };
        } catch (error) {
            return { success: false, error: error.message };
        }
    }

    static async actualizarEstado(numeroSerie, nuevoEstado, motivo, usuario, costoReparacion, costoMantenimiento) {
        try {
            const maquina = await modelsMaquina.findOne({ numeroSerie });
            if (!maquina) {
                return { success: false, error: 'Máquina no encontrada' };
            }

            const estadoAnterior = maquina.estado;
            maquina.estado = nuevoEstado;

            // Si se proporcionan costos, actualizarlos
            if (costoReparacion !== undefined && costoReparacion > 0) {
                maquina.costoReparacion = (maquina.costoReparacion || 0) + costoReparacion;
            }
            if (costoMantenimiento !== undefined && costoMantenimiento > 0) {
                maquina.costoMantenimiento = (maquina.costoMantenimiento || 0) + costoMantenimiento;
            }

            maquina.historialEstados.push({
                estado: nuevoEstado,
                fechaCambio: new Date(),
                motivo: motivo || `Cambio de ${estadoAnterior} a ${nuevoEstado}`,
                usuario: usuario
            });

            await this.actualizarNotificaciones(maquina);
            await maquina.save();

            return { success: true, data: maquina };
        } catch (error) {
            return { success: false, error: error.message };
        }
    }

    static async registrarHorasUso(numeroSerie, horasAdicionales) {
        try {
            const maquina = await modelsMaquina.findOne({ numeroSerie });
            if (!maquina) {
                return { success: false, error: 'Máquina no encontrada' };
            }

            maquina.horasUso += horasAdicionales;
            await this.actualizarNotificaciones(maquina);
            await maquina.save();

            return { success: true, data: maquina };
        } catch (error) {
            return { success: false, error: error.message };
        }
    }

    static async obtenerMaquinasConNotificaciones() {
        try {
            const maquinas = await modelsMaquina.find({
                $or: [
                    { 'notificaciones.mantenimientoPendiente': true },
                    { 'notificaciones.garantiaPorVencer': true },
                    { 'notificaciones.reparacionNecesaria': true }
                ]
            }).populate('proveedor');

            return { success: true, data: maquinas };
        } catch (error) {
            return { success: false, error: error.message };
        }
    }

    static async obtenerEstadisticasGenerales() {
        try {
            const totalMaquinas = await modelsMaquina.countDocuments();
            const operativas = await modelsMaquina.countDocuments({ estado: 'operativa' });
            const enReparacion = await modelsMaquina.countDocuments({ estado: 'en reparación' });
            const fueraServicio = await modelsMaquina.countDocuments({ estado: 'fuera de servicio' });
            const mantenimiento = await modelsMaquina.countDocuments({ estado: 'mantenimiento' });

            const maquinasPorTipo = await modelsMaquina.aggregate([
                { $group: { _id: '$tipo', cantidad: { $sum: 1 } } }
            ]);

            return {
                success: true,
                data: {
                    total: totalMaquinas,
                    estados: {
                        operativas,
                        enReparacion,
                        fueraServicio,
                        mantenimiento
                    },
                    porTipo: maquinasPorTipo
                }
            };
        } catch (error) {
            return { success: false, error: error.message };
        }
    }

    static async programarMantenimiento(numeroSerie, fechaMantenimiento, usuario) {
        try {
            const maquina = await modelsMaquina.findOne({ numeroSerie });
            if (!maquina) {
                return { success: false, error: 'Máquina no encontrada' };
            }

            maquina.mantenimientoProgramado = fechaMantenimiento;
            maquina.historialEstados.push({
                estado: maquina.estado,
                fechaCambio: new Date(),
                motivo: `Mantenimiento programado para ${fechaMantenimiento}`,
                usuario: usuario
            });

            await this.actualizarNotificaciones(maquina);
            await maquina.save();

            return { success: true, data: maquina };
        } catch (error) {
            return { success: false, error: error.message };
        }
    }

    static async actualizarNotificaciones(maquina) {
        const ahora = new Date();
        const treintaDias = new Date(ahora.getTime() + (30 * 24 * 60 * 60 * 1000));

        if (maquina.mantenimientoProgramado && maquina.mantenimientoProgramado <= treintaDias) {
            maquina.notificaciones.mantenimientoPendiente = true;
        }

        if (maquina.garantia && maquina.garantia.fechaFin && maquina.garantia.fechaFin <= treintaDias) {
            maquina.notificaciones.garantiaPorVencer = true;
        }

        if (maquina.estado === 'en reparación') {
            maquina.notificaciones.reparacionNecesaria = true;
        } else {
            maquina.notificaciones.reparacionNecesaria = false;
        }
    }

    static async buscarMaquinas(filtros) {
        try {
            const condiciones = {};
            
            if (filtros.tipo) condiciones.tipo = new RegExp(filtros.tipo, 'i');
            if (filtros.marca) condiciones.marca = new RegExp(filtros.marca, 'i');
            if (filtros.estado) condiciones.estado = filtros.estado;
            if (filtros.ubicacion) condiciones.ubicacion = new RegExp(filtros.ubicacion, 'i');
            if (filtros.proveedor) condiciones.proveedor = filtros.proveedor;

            const maquinas = await modelsMaquina.find(condiciones)
                .populate('proveedor')
                .sort({ createdAt: -1 });

            return { success: true, data: maquinas };
        } catch (error) {
            return { success: false, error: error.message };
        }
    }
}

module.exports = MaquinaService;