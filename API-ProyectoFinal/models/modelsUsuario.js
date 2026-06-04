const mongoose = require('mongoose');

const usuarioSchema = new mongoose.Schema({
    dni: { type: String, required: true, unique: true },
    nombre: { type: String, required: true },
    apellidos: { type: String, required: true },
    email: { 
        type: String, 
        required: true, 
        unique: true,
        validate: {
            validator: function(email) {
                // Expresión regular para validar correos con dominios específicos
                const emailRegex = /^[a-zA-Z0-9._-]+@(gmail|outlook|yahoo|hotmail|live|alu\.edu\.gva)\.(com|es|net|org|edu)$/i;
                return emailRegex.test(email);
            },
            message: 'El formato del correo no es válido. Debe usar dominios como @gmail.com, @outlook.es, @yahoo.com, o correos corporativos como @alu.edu.gva.es'
        }
    },
    password: { type: String, required: true },
rol: { type: String, enum: ['cliente', 'administrador', 'empleado'], default: 'cliente' },
    fechaNacimiento: { type: Date, required: true },
    ciudad: { 
        type: String,
        validate: {
            validator: function(ciudad) {
                if (!ciudad) return true; // Campo opcional
                
                // Lista de ciudades españolas principales
                const ciudadesEspanolas = [
                    'madrid', 'barcelona', 'valencia', 'sevilla', 'zaragoza', 'málaga', 'murcia', 
                    'palma', 'las palmas de gran canaria', 'bilbao', 'alicante', 'córdoba', 'valladolid',
                    'vigo', 'gijón', 'hospitalet de llobregat', 'vitoria', 'granada', 'elche', 'oviedo',
                    'badalona', 'cartagena', 'terrassa', 'jerez de la frontera', 'sabadell', 'móstoles',
                    'santa cruz de tenerife', 'pamplona', 'almería', 'burgos', 'albacete', 'getafe',
                    'santander', 'castellón de la plana', 'logroño', 'badajoz', 'huelva', 'salamanca',
                    'lleida', 'tarragona', 'león', 'cádiz', 'dos hermanas', 'marbella', 'ourense',
                    'torrejón de ardoz', 'parla', 'alcorcón', 'reus', 'telde', 'lugo', 'santiago de compostela',
                    'cáceres', 'lorca', 'coslada', 'talavera de la reina', 'el puerto de santa maría',
                    'cornellà de llobregat', 'avilés', 'palencia', 'gava', 'algeciras', 'alcalá de guadaíra'
                ];
                
                return ciudadesEspanolas.includes(ciudad.toLowerCase().trim());
            },
            message: 'La ciudad debe ser una ciudad española válida'
        }
    },
    telefono: { type: String },
    reputacion: { type: Number, default: 0 },
    descuentoActual: { type: Number, default: 0, min: 0, max: 100 },
    membresia: {
        activa: { type: Boolean, default: false },
        tipo: { type: String, enum: ['basica', 'premium', 'vip'], default: 'basica' },
        fechaInicio: { type: Date },
        fechaFin: { type: Date },
        descuentoPorPermanencia: { type: Number, default: 0 }
    },
    imagenPerfil: { type: String }
}, {
    timestamps: true
});

module.exports = mongoose.model('modelsUsuario', usuarioSchema, 'usuarios');
