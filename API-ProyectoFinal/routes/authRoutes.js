const express = require('express');
const router = express.Router();
const jwt = require('jsonwebtoken');
const modelsUsuario = require('../models/modelsUsuario');
const bcrypt = require('bcrypt');

// LOGIN
router.post('/login', async (req, res) => {
    const { email, password } = req.body;

    // Validación de entrada
    if (!email || typeof email !== 'string' || !email.trim()) {
        return res.status(400).json({ message: 'El email es requerido' });
    }
    if (!password || typeof password !== 'string' || !password.trim()) {
        return res.status(400).json({ message: 'La contraseña es requerida' });
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
        return res.status(400).json({ message: 'Formato de email inválido' });
    }

    try {
        const usuario = await modelsUsuario.findOne({ email: email.trim().toLowerCase() });
        if (!usuario) return res.status(401).json({ message: 'Credenciales inválidas' });

        const isValid = await bcrypt.compare(password.trim(), usuario.password);
        if (!isValid) return res.status(401).json({ message: 'Credenciales inválidas' });

        const payload = {
            id: usuario._id,
            email: usuario.email,
            rol: usuario.rol,
            dni: usuario.dni
        };

        const token = jwt.sign(payload, process.env.JWT_SECRET, { expiresIn: '1h' });

        res.json({ token });

    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// Valida formato DNI español
function isValidDni(dni) {
    const letras = 'TRWAGMYFPDXBNJZSQVHLCKE';
    const pattern = /^\d{8}[A-Za-z]$/;
    if (!pattern.test(dni)) return false;
    return dni[8].toUpperCase() === letras[parseInt(dni.substring(0, 8)) % 23];
}

// REGISTRO
router.post('/register', async (req, res) => {
    const { name, email, password, dni, ciudad, fechaNacimiento } = req.body;

    // Validación de entrada
    if (!name || typeof name !== 'string' || name.trim().length < 2) {
        return res.status(400).json({ message: 'El nombre debe tener al menos 2 caracteres' });
    }
    if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
        return res.status(400).json({ message: 'Formato de email inválido' });
    }
    if (!password || typeof password !== 'string' || password.trim().length < 6) {
        return res.status(400).json({ message: 'La contraseña debe tener al menos 6 caracteres' });
    }
    if (!dni || !isValidDni(dni.trim())) {
        return res.status(400).json({ message: 'DNI inválido (formato: 12345678Z)' });
    }
    if (!fechaNacimiento || !/^\d{2}\/\d{2}\/\d{4}$/.test(fechaNacimiento.trim())) {
        return res.status(400).json({ message: 'Fecha inválida. Use el formato DD/MM/YYYY' });
    }

    console.log('=== DATOS DE REGISTRO RECIBIDOS ===');
    console.log('Name:', name);
    console.log('Email:', email);
    console.log('DNI:', dni);
    console.log('Ciudad recibida:', ciudad);
    console.log('Fecha Nacimiento:', fechaNacimiento);
    console.log('===================================');

    try {
        // Validar que los campos requeridos estén presentes
        if (!name || !email || !password || !dni || !ciudad || !fechaNacimiento) {
            return res.status(400).json({ message: 'Todos los campos son requeridos' });
        }

        // Verificar si el usuario ya existe
        const usuarioExistente = await modelsUsuario.findOne({ email });
        if (usuarioExistente) {
            return res.status(400).json({ message: 'El email ya está registrado' });
        }

        // Verificar si el DNI ya existe
        const dniExistente = await modelsUsuario.findOne({ dni });
        if (dniExistente) {
            return res.status(400).json({ message: 'El DNI ya está registrado' });
        }

        // Encriptar contraseña
        const hashedPassword = await bcrypt.hash(password, 10);

        // Separar nombre en nombre y apellidos (si es posible)
        const nombreParts = name.trim().split(' ');
        const nombre = nombreParts[0];
        const apellidos = nombreParts.slice(1).join(' ') || nombre;

        // Convertir fecha de DD/MM/YYYY a Date
        const [dia, mes, anio] = fechaNacimiento.split('/');
        const fechaNacimientoDate = new Date(anio, mes - 1, dia);

        // Crear nuevo usuario
        const ciudadProcesada = ciudad.toLowerCase().trim();
        console.log('Ciudad procesada para guardar:', ciudadProcesada);

        const nuevoUsuario = new modelsUsuario({
            dni: dni,
            nombre: nombre,
            apellidos: apellidos,
            email: email,
            password: hashedPassword,
            rol: 'cliente',
            fechaNacimiento: fechaNacimientoDate,
            ciudad: ciudadProcesada
        });

        console.log('Usuario a guardar - ciudad:', nuevoUsuario.ciudad);
        await nuevoUsuario.save();
        console.log('Usuario guardado - ciudad:', nuevoUsuario.ciudad);

        // Generar token
        const payload = {
            id: nuevoUsuario._id,
            email: nuevoUsuario.email,
            rol: nuevoUsuario.rol,
            dni: nuevoUsuario.dni
        };

        const token = jwt.sign(payload, process.env.JWT_SECRET, { expiresIn: '1h' });

        res.status(201).json({
            token,
            user: {
                id: nuevoUsuario._id,
                name: nuevoUsuario.nombre,
                email: nuevoUsuario.email
            }
        });

    } catch (error) {
        if (error.name === 'ValidationError') {
            return res.status(400).json({ message: error.message });
        }
        res.status(500).json({ message: 'Error al registrar usuario: ' + error.message });
    }
});

module.exports = router;
