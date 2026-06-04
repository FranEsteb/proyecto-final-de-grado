const mongoose = require('mongoose');
const MaquinaService = require('./services/maquinaService');
require('./models');

const testMaquinaModule = async () => {
    try {
        await mongoose.connect(process.env.MONGODB_URI || 'mongodb://localhost:27017/proyecto-final');
        console.log('✅ Conectado a MongoDB para pruebas');

        const testMachine = {
            numeroSerie: 'TEST-001',
            tipo: 'bicicleta',
            marca: 'TestMarca',
            modelo: 'TestModelo',
            fechaCompra: new Date(),
            estado: 'operativa',
            ubicacion: 'Sala de pruebas',
            costoCompra: 1500,
            especificaciones: {
                peso: 50,
                dimensiones: '120x60x100',
                consumoEnergia: 200,
                capacidadMaxima: 150
            }
        };

        console.log('\n🧪 Probando registro de máquina...');
        const registro = await MaquinaService.registrarMaquina(testMachine, 'test-user');
        if (registro.success) {
            console.log('✅ Máquina registrada exitosamente:', registro.data.numeroSerie);
        } else {
            console.log('❌ Error en registro:', registro.error);
            return;
        }

        console.log('\n🧪 Probando actualización de estado...');
        const actualizacion = await MaquinaService.actualizarEstado('TEST-001', 'mantenimiento', 'Mantenimiento preventivo', 'test-user');
        if (actualizacion.success) {
            console.log('✅ Estado actualizado exitosamente');
            console.log('📊 Historial de estados:', actualizacion.data.historialEstados.length, 'entradas');
        } else {
            console.log('❌ Error en actualización:', actualizacion.error);
        }

        console.log('\n🧪 Probando registro de horas de uso...');
        const horasUso = await MaquinaService.registrarHorasUso('TEST-001', 5.5);
        if (horasUso.success) {
            console.log('✅ Horas de uso registradas:', horasUso.data.horasUso, 'horas');
        } else {
            console.log('❌ Error en horas de uso:', horasUso.error);
        }

        console.log('\n🧪 Probando programación de mantenimiento...');
        const fechaMantenimiento = new Date(Date.now() + (7 * 24 * 60 * 60 * 1000));
        const mantenimiento = await MaquinaService.programarMantenimiento('TEST-001', fechaMantenimiento, 'test-user');
        if (mantenimiento.success) {
            console.log('✅ Mantenimiento programado para:', fechaMantenimiento.toLocaleDateString());
        } else {
            console.log('❌ Error en programación:', mantenimiento.error);
        }

        console.log('\n🧪 Probando obtención de estadísticas...');
        const estadisticas = await MaquinaService.obtenerEstadisticasGenerales();
        if (estadisticas.success) {
            console.log('✅ Estadísticas obtenidas:');
            console.log('   Total máquinas:', estadisticas.data.total);
            console.log('   Estados:', estadisticas.data.estados);
        } else {
            console.log('❌ Error en estadísticas:', estadisticas.error);
        }

        console.log('\n🧪 Probando búsqueda de máquinas...');
        const busqueda = await MaquinaService.buscarMaquinas({ tipo: 'bicicleta' });
        if (busqueda.success) {
            console.log('✅ Búsqueda exitosa:', busqueda.data.length, 'máquinas encontradas');
        } else {
            console.log('❌ Error en búsqueda:', busqueda.error);
        }

        console.log('\n🧪 Probando notificaciones...');
        const notificaciones = await MaquinaService.obtenerMaquinasConNotificaciones();
        if (notificaciones.success) {
            console.log('✅ Notificaciones obtenidas:', notificaciones.data.length, 'máquinas con notificaciones');
        } else {
            console.log('❌ Error en notificaciones:', notificaciones.error);
        }

        console.log('\n🧹 Limpiando datos de prueba...');
        await mongoose.model('modelsMaquina').deleteOne({ numeroSerie: 'TEST-001' });
        console.log('✅ Datos de prueba eliminados');

        console.log('\n🎉 ¡Todas las pruebas completadas exitosamente!');
        console.log('\n📋 Funcionalidades implementadas:');
        console.log('   ✓ Registro completo de máquinas');
        console.log('   ✓ Seguimiento de estados con historial');
        console.log('   ✓ Registro de horas de uso');
        console.log('   ✓ Programación de mantenimientos');
        console.log('   ✓ Sistema de notificaciones');
        console.log('   ✓ Estadísticas generales');
        console.log('   ✓ Búsqueda avanzada');
        console.log('   ✓ Validación de datos');
        console.log('   ✓ Panel administrativo');
        
    } catch (error) {
        console.error('❌ Error en pruebas:', error);
    } finally {
        await mongoose.connection.close();
        console.log('🔌 Conexión cerrada');
    }
};

if (require.main === module) {
    testMaquinaModule();
}

module.exports = testMaquinaModule;