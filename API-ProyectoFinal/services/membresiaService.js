const modelsMembresia = require('../models/modelsMembresia');
const modelsUsuario = require('../models/modelsUsuario');

class MembresiaService {
    
    // Actualizar descuentos por permanencia para todas las membresías activas
    static async actualizarDescuentosPermanencia() {
        try {
            console.log('🔄 Iniciando actualización de descuentos por permanencia...');
            
            const membresiasActivas = await modelsMembresia.find({ 
                estado: 'activa',
                fechaFin: { $gt: new Date() }
            });
            
            let actualizadas = 0;
            
            for (const membresia of membresiasActivas) {
                try {
                    const descuentoAnterior = membresia.descuentoActual || 0;
                    const nuevoDescuento = membresia.calcularDescuentoPorPermanencia();
                    
                    if (nuevoDescuento !== descuentoAnterior) {
                        // Actualizar en la membresía
                        membresia.descuentoActual = nuevoDescuento;
                        await membresia.save();
                        
                        // Actualizar en el usuario
                        await modelsUsuario.findByIdAndUpdate(membresia.usuario, {
                            'membresia.descuentoPorPermanencia': nuevoDescuento
                        });
                        
                        actualizadas++;
                        console.log(`✅ Usuario ${membresia.usuario}: ${descuentoAnterior}% → ${nuevoDescuento}%`);
                    }
                } catch (error) {
                    console.error(`❌ Error actualizando membresía ${membresia._id}:`, error.message);
                }
            }
            
            console.log(`✨ Actualización completada: ${actualizadas} membresías actualizadas de ${membresiasActivas.length} total`);
            return {
                total: membresiasActivas.length,
                actualizadas,
                mensaje: `Se actualizaron ${actualizadas} descuentos por permanencia`
            };
            
        } catch (error) {
            console.error('❌ Error en actualización de descuentos por permanencia:', error);
            throw error;
        }
    }
    
    // Marcar membresías expiradas
    static async marcarMembresiasExpiradas() {
        try {
            console.log('🔄 Verificando membresías expiradas...');
            
            const result = await modelsMembresia.updateMany(
                {
                    estado: 'activa',
                    fechaFin: { $lte: new Date() }
                },
                {
                    estado: 'expirada'
                }
            );
            
            // Actualizar usuarios con membresías expiradas
            const membresiasExpiradas = await modelsMembresia.find({
                estado: 'expirada',
                fechaFin: { $lte: new Date() }
            });
            
            for (const membresia of membresiasExpiradas) {
                await modelsUsuario.findByIdAndUpdate(membresia.usuario, {
                    'membresia.activa': false,
                    'membresia.descuentoPorPermanencia': 0
                });
            }
            
            console.log(`📅 Se marcaron ${result.modifiedCount} membresías como expiradas`);
            return {
                expiradas: result.modifiedCount,
                mensaje: `Se marcaron ${result.modifiedCount} membresías como expiradas`
            };
            
        } catch (error) {
            console.error('❌ Error marcando membresías expiradas:', error);
            throw error;
        }
    }
    
    // Obtener estadísticas de membresías
    static async obtenerEstadisticas() {
        try {
            const estadisticas = await modelsMembresia.aggregate([
                {
                    $group: {
                        _id: "$estado",
                        count: { $sum: 1 }
                    }
                }
            ]);
            
            const estadisticasPorTipo = await modelsMembresia.aggregate([
                {
                    $match: { estado: 'activa' }
                },
                {
                    $group: {
                        _id: "$tipo",
                        count: { $sum: 1 }
                    }
                }
            ]);
            
            // Membresías próximas a vencer (próximos 7 días)
            const proximasAVencer = await modelsMembresia.countDocuments({
                estado: 'activa',
                fechaFin: {
                    $gte: new Date(),
                    $lte: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
                }
            });
            
            return {
                estadisticasGenerales: estadisticas,
                estadisticasPorTipo,
                proximasAVencer
            };
            
        } catch (error) {
            console.error('❌ Error obteniendo estadísticas:', error);
            throw error;
        }
    }
    
    // Ejecutar mantenimiento completo
    static async ejecutarMantenimiento() {
        try {
            console.log('🛠️ Iniciando mantenimiento de membresías...');
            
            const resultadoExpiradas = await this.marcarMembresiasExpiradas();
            const resultadoDescuentos = await this.actualizarDescuentosPermanencia();
            const estadisticas = await this.obtenerEstadisticas();
            
            console.log('✅ Mantenimiento de membresías completado');
            
            return {
                expiradas: resultadoExpiradas,
                descuentos: resultadoDescuentos,
                estadisticas,
                timestamp: new Date()
            };
            
        } catch (error) {
            console.error('❌ Error en mantenimiento de membresías:', error);
            throw error;
        }
    }
}

module.exports = MembresiaService;