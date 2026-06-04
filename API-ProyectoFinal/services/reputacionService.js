const modelsReputacion = require('../models/modelsReputacion');
const modelsUsuario = require('../models/modelsUsuario');

class ReputacionService {
    
    // Aplicar cambio de reputación y actualizar el usuario
    static async aplicarCambioReputacion(usuarioId, tipo, motivo, puntos, aplicadoPor = null) {
        try {
            console.log(`🎯 Aplicando cambio de reputación: ${puntos} puntos (${tipo}) a usuario ${usuarioId}`);

            // Obtener usuario actual para logging
            const usuarioActual = await modelsUsuario.findById(usuarioId);
            console.log(`📊 Usuario actual: ${usuarioActual.nombre}, reputación antes: ${usuarioActual.reputacion}`);

            // Crear registro de reputación
            const idRep = `REP_${Date.now()}_${Math.random().toString(36).substr(2, 5)}`;
            
            const nuevaReputacion = new modelsReputacion({
                idRep,
                usuario: usuarioId,
                tipo, // 'recompensa' o 'sancion'
                motivo,
                puntos: Math.abs(puntos), // Siempre positivo en el registro
                fecha: new Date()
            });

            console.log(`💾 Guardando registro de reputación: ${JSON.stringify(nuevaReputacion)}`);
            await nuevaReputacion.save();
            console.log(`✅ Registro guardado con ID: ${nuevaReputacion._id}`);

            // Calcular nuevo total de reputación
            const nuevoTotal = await this.calcularReputacionTotal(usuarioId);
            console.log(`🧮 Nuevo total calculado: ${nuevoTotal}`);

            // Actualizar usuario con nueva reputación
            console.log(`🔄 Actualizando usuario ${usuarioId} con reputación ${nuevoTotal}`);
            const resultadoUpdate = await modelsUsuario.findByIdAndUpdate(
                usuarioId, 
                { reputacion: nuevoTotal },
                { new: true, runValidators: true }
            );

            console.log(`✅ Usuario actualizado: ${resultadoUpdate.nombre}, nueva reputación: ${resultadoUpdate.reputacion}`);

            // Verificación adicional
            const verificacion = await modelsUsuario.findById(usuarioId);
            console.log(`🔍 Verificación final: reputación en BD = ${verificacion.reputacion}`);

            return {
                registroCreado: nuevaReputacion,
                nuevaReputacion: nuevoTotal,
                reputacionAnterior: usuarioActual.reputacion,
                usuarioActualizado: resultadoUpdate,
                mensaje: `Reputación ${tipo === 'recompensa' ? 'aumentada' : 'reducida'} en ${Math.abs(puntos)} puntos`
            };

        } catch (error) {
            console.error('❌ Error aplicando cambio de reputación:', error);
            console.error('Stack trace:', error.stack);
            throw error;
        }
    }

    // Calcular reputación total basada en todos los registros
    static async calcularReputacionTotal(usuarioId) {
        try {
            console.log(`📊 Calculando reputación total para usuario: ${usuarioId}`);
            const registros = await modelsReputacion.find({ usuario: usuarioId });
            console.log(`📋 Encontrados ${registros.length} registros de reputación`);
            
            let total = 0;
            let recompensas = 0;
            let sanciones = 0;
            
            registros.forEach(registro => {
                console.log(`📝 Procesando: ${registro.tipo} ${registro.puntos} - ${registro.motivo}`);
                if (registro.tipo === 'recompensa') {
                    total += registro.puntos;
                    recompensas += registro.puntos;
                } else if (registro.tipo === 'sancion') {
                    total -= registro.puntos;
                    sanciones += registro.puntos;
                }
            });

            console.log(`➕ Total recompensas: ${recompensas}`);
            console.log(`➖ Total sanciones: ${sanciones}`);
            console.log(`🎯 Total calculado: ${total}`);

            // Asegurar que no sea negativo
            const resultado = Math.max(0, total);
            console.log(`✅ Resultado final: ${resultado}`);
            
            return resultado;

        } catch (error) {
            console.error('❌ Error calculando reputación total:', error);
            throw error;
        }
    }

    // Eventos automáticos de reputación
    static async eventoCompletarReserva(usuarioId, esATime = true) {
        const puntos = esATime ? 5 : 2; // 5 puntos si llegó a tiempo, 2 si llegó tarde
        const motivo = esATime ? 'Asistió puntualmente a reserva' : 'Completó reserva (llegada tardía)';
        
        return await this.aplicarCambioReputacion(usuarioId, 'recompensa', motivo, puntos);
    }

    static async eventoCancelarReserva(usuarioId, horasAnticipacion) {
        let puntos, motivo;
        
        if (horasAnticipacion >= 24) {
            puntos = 0; // Sin penalización
            motivo = 'Cancelación con más de 24h de anticipación';
        } else if (horasAnticipacion >= 2) {
            puntos = 2;
            motivo = 'Cancelación tardía (menos de 24h)';
        } else {
            puntos = 5;
            motivo = 'Cancelación de último momento (menos de 2h)';
        }

        if (puntos > 0) {
            return await this.aplicarCambioReputacion(usuarioId, 'sancion', motivo, puntos);
        }
        
        return { mensaje: 'Cancelación sin penalización', nuevaReputacion: await this.calcularReputacionTotal(usuarioId) };
    }

    static async eventoNoShow(usuarioId) {
        return await this.aplicarCambioReputacion(usuarioId, 'sancion', 'No se presentó a la reserva (No-show)', 10);
    }

    static async eventoCompletarMembresiaAnual(usuarioId) {
        return await this.aplicarCambioReputacion(usuarioId, 'recompensa', 'Completó un año de membresía activa', 20);
    }

    static async eventoUsarInstalacionesRegularmente(usuarioId) {
        return await this.aplicarCambioReputacion(usuarioId, 'recompensa', 'Uso regular de instalaciones (bonus mensual)', 3);
    }

    // Recompensa manual por admin/empleado
    static async aplicarRecompensaManual(usuarioId, motivo, puntos, aplicadoPor) {
        return await this.aplicarCambioReputacion(usuarioId, 'recompensa', `Manual: ${motivo}`, puntos, aplicadoPor);
    }

    // Sanción manual por admin/empleado
    static async aplicarSancionManual(usuarioId, motivo, puntos, aplicadoPor) {
        return await this.aplicarCambioReputacion(usuarioId, 'sancion', `Manual: ${motivo}`, puntos, aplicadoPor);
    }

    // Obtener historial de reputación de un usuario
    static async obtenerHistorialReputacion(usuarioId) {
        try {
            const historial = await modelsReputacion.find({ usuario: usuarioId })
                .sort({ fecha: -1 })
                .limit(50); // Últimos 50 registros

            const reputacionTotal = await this.calcularReputacionTotal(usuarioId);

            // Contar estadísticas
            const totalRecompensas = historial.filter(r => r.tipo === 'recompensa').length;
            const totalSanciones = historial.filter(r => r.tipo === 'sancion').length;
            const puntosGanados = historial.filter(r => r.tipo === 'recompensa').reduce((sum, r) => sum + r.puntos, 0);
            const puntosPerdidos = historial.filter(r => r.tipo === 'sancion').reduce((sum, r) => sum + r.puntos, 0);
            const ultimaActividad = historial.length > 0 ? historial[0].fecha : null;

            return {
                Registros: historial.map(registro => ({
                    IdRep: registro.idRep,
                    Fecha: registro.fecha,
                    Tipo: registro.tipo,
                    Motivo: registro.motivo,
                    Puntos: registro.tipo === 'recompensa' ? registro.puntos : -registro.puntos
                })),
                Resumen: {
                    TotalRegistros: historial.length,
                    TotalRecompensas: totalRecompensas,
                    TotalSanciones: totalSanciones,
                    PuntosGanados: puntosGanados,
                    PuntosPerdidos: puntosPerdidos,
                    UltimaActividad: ultimaActividad
                }
            };

        } catch (error) {
            console.error('❌ Error obteniendo historial de reputación:', error);
            throw error;
        }
    }

    // Sincronizar todas las reputaciones (mantenimiento)
    static async sincronizarTodasLasReputaciones() {
        try {
            console.log('🔄 Iniciando sincronización de reputaciones...');
            
            const usuarios = await modelsUsuario.find({});
            let actualizados = 0;

            for (const usuario of usuarios) {
                try {
                    const reputacionCalculada = await this.calcularReputacionTotal(usuario._id);
                    
                    if (usuario.reputacion !== reputacionCalculada) {
                        await modelsUsuario.findByIdAndUpdate(usuario._id, {
                            reputacion: reputacionCalculada
                        });
                        console.log(`✅ Usuario ${usuario.nombre}: ${usuario.reputacion} → ${reputacionCalculada}`);
                        actualizados++;
                    }
                } catch (error) {
                    console.error(`❌ Error sincronizando usuario ${usuario._id}:`, error.message);
                }
            }

            console.log(`✨ Sincronización completada: ${actualizados} usuarios actualizados`);
            return {
                usuariosTotales: usuarios.length,
                usuariosActualizados: actualizados,
                mensaje: `Sincronización completada: ${actualizados} reputaciones actualizadas`
            };

        } catch (error) {
            console.error('❌ Error en sincronización de reputaciones:', error);
            throw error;
        }
    }
}

module.exports = ReputacionService;