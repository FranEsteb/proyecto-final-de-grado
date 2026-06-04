const modelsUsuario = require('../models/modelsUsuario');
const modelsMembresia = require('../models/modelsMembresia');
const modelsDescuento = require('../models/modelsDescuento');

class DescuentoService {

    // Calcular precio final con todos los descuentos aplicables
    static async calcularPrecioConDescuentos(usuarioId, precioOriginal, tipoTransaccion = 'general') {
        try {
            console.log(`💰 Calculando descuentos para usuario ${usuarioId}, precio original: €${precioOriginal}`);

            const usuario = await modelsUsuario.findById(usuarioId);
            if (!usuario) {
                throw new Error('Usuario no encontrado');
            }

            let descuentos = [];
            let precioFinal = precioOriginal;

            // 1. Descuento por membresía y permanencia
            const descuentoMembresia = await this.obtenerDescuentoMembresia(usuarioId);
            if (descuentoMembresia.porcentaje > 0) {
                descuentos.push(descuentoMembresia);
                precioFinal *= (100 - descuentoMembresia.porcentaje) / 100;
            }

            // 2. Descuentos activos temporales
            const descuentosActivos = await this.obtenerDescuentosActivos(usuarioId);
            for (const descuento of descuentosActivos) {
                descuentos.push(descuento);
                precioFinal *= (100 - descuento.porcentaje) / 100;
            }

            // 3. Descuento por reputación alta
            const descuentoReputacion = this.calcularDescuentoPorReputacion(usuario.reputacion);
            if (descuentoReputacion.porcentaje > 0) {
                descuentos.push(descuentoReputacion);
                precioFinal *= (100 - descuentoReputacion.porcentaje) / 100;
            }

            // 4. Aplicar límite máximo de descuento (no más del 50%)
            const descuentoTotalPorcentaje = ((precioOriginal - precioFinal) / precioOriginal) * 100;
            if (descuentoTotalPorcentaje > 50) {
                precioFinal = precioOriginal * 0.5; // Máximo 50% descuento
                console.log('⚠️ Descuento limitado al 50% máximo');
            }

            const ahorroTotal = precioOriginal - precioFinal;
            const descuentoFinalPorcentaje = (ahorroTotal / precioOriginal) * 100;

            console.log(`✅ Precio calculado: €${precioOriginal} → €${precioFinal.toFixed(2)} (${descuentoFinalPorcentaje.toFixed(1)}% descuento)`);

            return {
                precioOriginal: parseFloat(precioOriginal.toFixed(2)),
                precioFinal: parseFloat(precioFinal.toFixed(2)),
                ahorroTotal: parseFloat(ahorroTotal.toFixed(2)),
                descuentoPorcentaje: parseFloat(descuentoFinalPorcentaje.toFixed(1)),
                descuentosAplicados: descuentos,
                usuario: {
                    nombre: usuario.nombre,
                    apellidos: usuario.apellidos,
                    reputacion: usuario.reputacion
                }
            };

        } catch (error) {
            console.error('❌ Error calculando precio con descuentos:', error);
            throw error;
        }
    }

    // Obtener descuento por membresía y permanencia
    static async obtenerDescuentoMembresia(usuarioId) {
        try {
            const membresia = await modelsMembresia.obtenerMembresiaActiva(usuarioId);
            
            if (!membresia || membresia.estado !== 'activa') {
                return { tipo: 'Sin membresía', porcentaje: 0, detalle: 'Usuario sin membresía activa' };
            }

            const descuentoPermanencia = membresia.calcularDescuentoPorPermanencia();
            let bonusTipo = 0;

            // Bonus adicional por tipo de membresía
            switch (membresia.tipo) {
                case 'premium':
                    bonusTipo = 5;
                    break;
                case 'vip':
                    bonusTipo = 10;
                    break;
                default:
                    bonusTipo = 0;
            }

            const descuentoTotal = Math.min(descuentoPermanencia + bonusTipo, 30); // Máximo 30% por membresía

            return {
                tipo: 'Membresía',
                porcentaje: descuentoTotal,
                detalle: `${membresia.tipo.toUpperCase()}: ${descuentoPermanencia}% permanencia + ${bonusTipo}% tipo`,
                tipoMembresia: membresia.tipo,
                permanencia: descuentoPermanencia,
                bonus: bonusTipo
            };

        } catch (error) {
            console.error('❌ Error obteniendo descuento de membresía:', error);
            return { tipo: 'Error', porcentaje: 0, detalle: 'Error al calcular descuento de membresía' };
        }
    }

    // Obtener descuentos temporales activos
    static async obtenerDescuentosActivos(usuarioId) {
        try {
            const ahora = new Date();
            const usuario = await modelsUsuario.findById(usuarioId);
            
            if (!usuario || !usuario.descuentosActivos) {
                return [];
            }

            return usuario.descuentosActivos
                .filter(descuento => {
                    return descuento.fechaFin > ahora && descuento.fechaInicio <= ahora;
                })
                .map(descuento => ({
                    tipo: 'Descuento temporal',
                    porcentaje: descuento.porcentaje,
                    detalle: descuento.tipo,
                    validoHasta: descuento.fechaFin
                }));

        } catch (error) {
            console.error('❌ Error obteniendo descuentos activos:', error);
            return [];
        }
    }

    // Calcular descuento por reputación
    static calcularDescuentoPorReputacion(reputacion) {
        let porcentaje = 0;
        let detalle = '';

        if (reputacion >= 100) {
            porcentaje = 10;
            detalle = 'Reputación excelente (≥100 puntos)';
        } else if (reputacion >= 80) {
            porcentaje = 7;
            detalle = 'Reputación muy buena (80-99 puntos)';
        } else if (reputacion >= 60) {
            porcentaje = 5;
            detalle = 'Reputación buena (60-79 puntos)';
        } else if (reputacion >= 40) {
            porcentaje = 3;
            detalle = 'Reputación regular (40-59 puntos)';
        } else {
            porcentaje = 0;
            detalle = 'Sin descuento por reputación';
        }

        return {
            tipo: 'Reputación',
            porcentaje,
            detalle,
            reputacion
        };
    }

    // Aplicar descuento temporal específico
    static async aplicarDescuentoTemporal(usuarioId, porcentaje, motivo, duracionDias, aplicadoPor) {
        try {
            const usuario = await modelsUsuario.findById(usuarioId);
            if (!usuario) {
                throw new Error('Usuario no encontrado');
            }

            const fechaInicio = new Date();
            const fechaFin = new Date();
            fechaFin.setDate(fechaFin.getDate() + duracionDias);

            // Crear registro en la colección de descuentos
            const descuentoId = `DESC_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`;
            const nuevoDescuento = new modelsDescuento({
                descuentoId,
                usuario: usuarioId,
                tipo: motivo,
                porcentaje,
                fechaInicio,
                fechaFin,
                motivo
            });

            await nuevoDescuento.save();

            // Agregar también al array de descuentos activos del usuario
            usuario.descuentosActivos.push({
                tipo: motivo,
                porcentaje,
                fechaInicio,
                fechaFin
            });

            await usuario.save();

            console.log(`✅ Descuento temporal aplicado: ${porcentaje}% por ${duracionDias} días a usuario ${usuarioId}`);

            return {
                descuento: nuevoDescuento,
                mensaje: `Descuento del ${porcentaje}% aplicado por ${duracionDias} días`,
                validoHasta: fechaFin
            };

        } catch (error) {
            console.error('❌ Error aplicando descuento temporal:', error);
            throw error;
        }
    }

    // Obtener resumen de descuentos para un usuario
    static async obtenerResumenDescuentos(usuarioId) {
        try {
            const usuario = await modelsUsuario.findById(usuarioId);
            if (!usuario) {
                throw new Error('Usuario no encontrado');
            }

            const descuentoMembresia = await this.obtenerDescuentoMembresia(usuarioId);
            const descuentosActivos = await this.obtenerDescuentosActivos(usuarioId);
            const descuentoReputacion = this.calcularDescuentoPorReputacion(usuario.reputacion);

            // Calcular descuento total estimado
            let descuentoTotalEstimado = 0;
            let precioEjemplo = 100; // Precio base ejemplo

            const resultado = await this.calcularPrecioConDescuentos(usuarioId, precioEjemplo);

            return {
                usuario: {
                    nombre: usuario.nombre,
                    apellidos: usuario.apellidos,
                    reputacion: usuario.reputacion
                },
                descuentos: {
                    membresia: descuentoMembresia,
                    temporales: descuentosActivos,
                    reputacion: descuentoReputacion
                },
                resumen: {
                    descuentoTotalPorcentaje: resultado.descuentoPorcentaje,
                    ejemploPrecio: {
                        original: resultado.precioOriginal,
                        final: resultado.precioFinal,
                        ahorro: resultado.ahorroTotal
                    }
                }
            };

        } catch (error) {
            console.error('❌ Error obteniendo resumen de descuentos:', error);
            throw error;
        }
    }

    // Limpiar descuentos expirados
    static async limpiarDescuentosExpirados() {
        try {
            console.log('🧹 Limpiando descuentos expirados...');
            
            const ahora = new Date();
            
            // Limpiar de la colección de descuentos
            const descuentosEliminados = await modelsDescuento.deleteMany({
                fechaFin: { $lt: ahora }
            });

            // Limpiar de los usuarios
            const usuarios = await modelsUsuario.find({
                'descuentosActivos.fechaFin': { $lt: ahora }
            });

            let usuariosActualizados = 0;

            for (const usuario of usuarios) {
                usuario.descuentosActivos = usuario.descuentosActivos.filter(
                    descuento => descuento.fechaFin > ahora
                );
                await usuario.save();
                usuariosActualizados++;
            }

            console.log(`✅ Limpieza completada: ${descuentosEliminados.deletedCount} registros eliminados, ${usuariosActualizados} usuarios actualizados`);

            return {
                descuentosEliminados: descuentosEliminados.deletedCount,
                usuariosActualizados,
                mensaje: `Limpieza completada: ${descuentosEliminados.deletedCount} descuentos expirados eliminados`
            };

        } catch (error) {
            console.error('❌ Error limpiando descuentos expirados:', error);
            throw error;
        }
    }
}

module.exports = DescuentoService;