require('dotenv').config();
require('./models');
const connectDB = require('./config/db');
const modelsUsuario = require('./models/modelsUsuario');
const bcrypt = require('bcrypt');

async function createTestUsers() {
    try {
        // Conectar a la base de datos
        await connectDB();
        console.log('Conectado a MongoDB');

        // Verificar si ya existen usuarios
        const existingUsers = await modelsUsuario.find();
        if (existingUsers.length > 0) {
            console.log('Ya existen usuarios en la base de datos:');
            existingUsers.forEach(user => {
                console.log(`- ${user.email} (${user.rol})`);
            });
            return;
        }

        // Crear usuarios de prueba
        const testUsers = [
            {
                dni: '12345678A',
                nombre: 'Admin',
                apellidos: 'Sistema',
                email: 'admin@test.com',
                password: 'admin123',
                rol: 'administrador',
                fechaNacimiento: new Date('1990-01-01'),
                ciudad: 'Madrid',
                telefono: '123456789'
            },
            {
                dni: '87654321B',
                nombre: 'Juan',
                apellidos: 'Pérez González',
                email: 'empleado@test.com',
                password: 'empleado123',
                rol: 'empleado',
                fechaNacimiento: new Date('1985-05-15'),
                ciudad: 'Barcelona',
                telefono: '987654321'
            },
            {
                dni: '11111111C',
                nombre: 'María',
                apellidos: 'García López',
                email: 'cliente@test.com',
                password: 'cliente123',
                rol: 'cliente',
                fechaNacimiento: new Date('1992-08-20'),
                ciudad: 'Valencia',
                telefono: '555666777'
            }
        ];

        console.log('Creando usuarios de prueba...');

        for (const userData of testUsers) {
            // Encriptar contraseña
            const hashedPassword = await bcrypt.hash(userData.password, 10);
            
            const user = new modelsUsuario({
                ...userData,
                password: hashedPassword
            });

            await user.save();
            console.log(`✅ Usuario creado: ${userData.email} (${userData.rol})`);
        }

        console.log('\n🎉 Usuarios de prueba creados exitosamente!');
        console.log('\nCredenciales de acceso:');
        console.log('📧 admin@test.com - Contraseña: admin123 (Administrador)');
        console.log('📧 empleado@test.com - Contraseña: empleado123 (Empleado)');
        console.log('📧 cliente@test.com - Contraseña: cliente123 (Cliente)');

        process.exit(0);
    } catch (error) {
        console.error('❌ Error al crear usuarios de prueba:', error);
        process.exit(1);
    }
}

createTestUsers();