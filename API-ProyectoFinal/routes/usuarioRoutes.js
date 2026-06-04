const express = require('express');
const bcrypt = require('bcrypt');
const modelsUsuario = require('../models/modelsUsuario');
const modelsMembresia = require('../models/modelsMembresia');
const ReputacionService = require('../services/reputacionService');
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// Función para validar formato de email
function isValidEmail(email) {
    const emailRegex = /^[a-zA-Z0-9._-]+@(gmail|outlook|yahoo|hotmail|live|alu\.edu\.gva)\.(com|es|net|org|edu)$/i;
    return emailRegex.test(email);
}

// Función para validar ciudades españolas
function isValidSpanishCity(ciudad) {
    if (!ciudad) return true; // Campo opcional
    
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
}

// GET ALL - Lista completa (WPF)
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const usuarios = await modelsUsuario.find();

        // Devolver usuarios con la membresía embebida y descuento calculado
        const usuariosObj = usuarios.map((usuario) => {
            const usuarioObj = usuario.toObject();

            // Si la membresía no existe, crear una por defecto
            if (!usuarioObj.membresia) {
                usuarioObj.membresia = {
                    activa: false,
                    tipo: 'basica',
                    fechaInicio: null,
                    fechaFin: null,
                    descuentoPorPermanencia: 0
                };
                usuarioObj.descuentoActual = 0;
            } else if (usuarioObj.membresia.activa && usuarioObj.membresia.fechaInicio) {
                // Calcular descuento automático por permanencia (2% por año, máximo 10%)
                const ahora = new Date();
                const mesesActivos = Math.floor((ahora - new Date(usuarioObj.membresia.fechaInicio)) / (1000 * 60 * 60 * 24 * 30));
                const anosActivos = Math.floor(mesesActivos / 12);
                const descuentoPermanencia = Math.min(anosActivos * 2, 10);

                // Actualizar el descuentoPorPermanencia en la membresía
                usuarioObj.membresia.descuentoPorPermanencia = descuentoPermanencia;

                // El descuento total es: descuentoActual (manual) + descuentoPorPermanencia (automático)
                const descuentoManual = usuarioObj.descuentoActual || 0;
                usuarioObj.descuentoActual = Math.min(descuentoManual + descuentoPermanencia, 100);
            }

            return usuarioObj;
        });

        res.status(200).json(usuariosObj);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// CREATE NEW USER (WPF)
router.post('/new', verifyToken, async (req, res) => {
    try {
        // Verificar permisos: empleados solo pueden crear clientes
        const currentUserRole = req.user.rol; // Del token JWT
        const requestedRole = req.body.Rol || req.body.rol;

        if (currentUserRole === 'empleado') {
            // Los empleados no pueden crear administradores ni empleados
            if (requestedRole && (requestedRole.toLowerCase() === 'administrador' || requestedRole.toLowerCase() === 'empleado')) {
                return res.status(403).json({
                    message: "No tienes permisos para crear un " + requestedRole + ". Los empleados solo pueden crear clientes."
                });
            }
        }

        // Manejar tanto PascalCase (WPF) como camelCase
        const password = req.body.Password || req.body.password;
        const dni = req.body.Dni || req.body.dni;
        const nombre = req.body.Nombre || req.body.nombre;
        const apellidos = req.body.Apellidos || req.body.apellidos;
        const email = req.body.Email || req.body.email;
        const rol = requestedRole || "cliente"; // Por defecto cliente si no se especifica o empleado intenta crear
        const fechaNacimiento = req.body.FechaNacimiento || req.body.fechaNacimiento;
        const ciudad = req.body.Ciudad || req.body.ciudad;

        // Validar formato de email
        if (email && !isValidEmail(email)) {
            return res.status(400).json({
                message: "El formato del correo no es válido. Debe usar dominios como @gmail.com, @outlook.es, @yahoo.com, o correos corporativos como @alu.edu.gva.es"
            });
        }

        // Validar ciudad española
        if (ciudad && !isValidSpanishCity(ciudad)) {
            return res.status(400).json({
                message: "La ciudad debe ser una ciudad española válida"
            });
        }
        const telefono = req.body.Telefono || req.body.telefono;
        const imagenPerfil = req.body.ImagenPerfil || req.body.imagenPerfil;
        const reputacion = req.body.Reputacion || req.body.reputacion || 0;

        if (!password || String(password).trim() === '') {
            return res.status(400).json({ message: "La contraseña es requerida" });
        }
        const hashedPassword = await bcrypt.hash(String(password).trim(), 10);
        const usuario = new modelsUsuario({
            dni,
            nombre,
            apellidos,
            email,
            password: hashedPassword,
            rol,
            fechaNacimiento,
            ciudad,
            telefono,
            reputacion,
            imagenPerfil
        });
        const savedUser = await usuario.save();
        res.status(200).json(savedUser);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// UPDATE USER by dni (WPF)
router.put('/update', verifyToken, async (req, res) => {
    try {
        // Manejar tanto PascalCase (WPF) como camelCase
        const dni = req.body.Dni || req.body.dni;
        if (!dni) return res.status(400).json({ message: "Falta el campo dni" });

        // Verificar si están intentando cambiar el DNI
        const newDni = req.body.NewDni || req.body.newDni || req.body.Dni || req.body.dni;
        if (req.body.NewDni !== undefined || req.body.newDni !== undefined || 
            (req.body.Dni && req.body.Dni !== dni) || (req.body.dni && req.body.dni !== dni)) {
            return res.status(400).json({ message: "El DNI no se puede modificar" });
        }

        // Obtener el usuario que se va a modificar
        const targetUser = await modelsUsuario.findOne({ dni });
        if (!targetUser) return res.status(404).json({ message: "Usuario no encontrado" });

        // Verificar permisos: empleado no puede modificar a administradores ni empleados
        const currentUserRole = req.user.rol; // Del token JWT
        if (currentUserRole === 'empleado') {
            if (targetUser.rol === 'administrador') {
                return res.status(403).json({ message: "No tienes permisos para modificar un administrador" });
            }
            if (targetUser.rol === 'empleado') {
                return res.status(403).json({ message: "No tienes permisos para modificar a otros empleados" });
            }
        }

        const updateFields = {};

        // Manejar campos con ambos formatos
        const nombre = req.body.Nombre !== undefined ? req.body.Nombre : req.body.nombre;
        const apellidos = req.body.Apellidos !== undefined ? req.body.Apellidos : req.body.apellidos;
        const email = req.body.Email !== undefined ? req.body.Email : req.body.email;
        
        // Validar formato de email si se proporciona
        if (email !== undefined && !isValidEmail(email)) {
            return res.status(400).json({ 
                message: "El formato del correo no es válido. Debe usar dominios como @gmail.com, @outlook.es, @yahoo.com, o correos corporativos como @alu.edu.gva.es" 
            });
        }
        
        const rol = req.body.Rol !== undefined ? req.body.Rol : req.body.rol;
        const fechaNacimiento = req.body.FechaNacimiento !== undefined ? req.body.FechaNacimiento : req.body.fechaNacimiento;
        const ciudad = req.body.Ciudad !== undefined ? req.body.Ciudad : req.body.ciudad;
        
        // Validar ciudad española si se proporciona
        if (ciudad !== undefined && !isValidSpanishCity(ciudad)) {
            return res.status(400).json({ 
                message: "La ciudad debe ser una ciudad española válida" 
            });
        }
        const telefono = req.body.Telefono !== undefined ? req.body.Telefono : req.body.telefono;
        const imagenPerfil = req.body.ImagenPerfil !== undefined ? req.body.ImagenPerfil : req.body.imagenPerfil;
        const reputacion = req.body.Reputacion !== undefined ? req.body.Reputacion : req.body.reputacion;
        const descuentoActual = req.body.DescuentoActual !== undefined ? req.body.DescuentoActual : req.body.descuentoActual;
        const motivo = req.body.Motivo !== undefined ? req.body.Motivo : req.body.motivo;
        const usuarioQueModifica = req.user?.email || 'Sistema';

        if (nombre !== undefined) updateFields.nombre = nombre;
        if (apellidos !== undefined) updateFields.apellidos = apellidos;
        if (email !== undefined) updateFields.email = email;
        if (rol !== undefined) updateFields.rol = rol;
        if (fechaNacimiento !== undefined) updateFields.fechaNacimiento = fechaNacimiento;
        if (ciudad !== undefined) updateFields.ciudad = ciudad;
        if (telefono !== undefined) updateFields.telefono = telefono;
        if (imagenPerfil !== undefined) updateFields.imagenPerfil = imagenPerfil;
        if (reputacion !== undefined) updateFields.reputacion = reputacion;
        const password = req.body.Password !== undefined ? req.body.Password : req.body.password;
        if (password !== undefined && password !== null && String(password).trim() !== '') {
            // Solo actualizar la contraseña si se proporciona un valor válido
            let cleanPassword = String(password).trim();
            if (!cleanPassword.startsWith('$2a$') && !cleanPassword.startsWith('$2b$')) {
                cleanPassword = await bcrypt.hash(cleanPassword, 10);
            }
            updateFields.password = cleanPassword;
        }

        // Manejar actualización de descuentoActual (reemplaza el anterior, no se acumula)
        if (descuentoActual !== undefined && descuentoActual !== null) {
            // Validar que sea un número entre 0 y 100
            const descuento = parseFloat(descuentoActual);
            if (isNaN(descuento) || descuento < 0 || descuento > 100) {
                return res.status(400).json({
                    message: "El descuentoActual debe ser un número entre 0 y 100"
                });
            }

            updateFields.descuentoActual = descuento;
        }

        // NUEVO: Manejar actualización de campos de membresía (membresia.descuentoPorPermanencia)
        const membresia = req.body.Membresia || req.body.membresia;
        if (membresia !== undefined && membresia !== null) {
            if (membresia.DescuentoPorPermanencia !== undefined || membresia.descuentoPorPermanencia !== undefined) {
                const descuento = membresia.DescuentoPorPermanencia !== undefined
                    ? membresia.DescuentoPorPermanencia
                    : membresia.descuentoPorPermanencia;
                updateFields['membresia.descuentoPorPermanencia'] = descuento;
            }
            // Agregar otros campos de membresía si se necesitan actualizar
            if (membresia.Activa !== undefined) updateFields['membresia.activa'] = membresia.Activa;
            if (membresia.activa !== undefined) updateFields['membresia.activa'] = membresia.activa;
            if (membresia.Tipo !== undefined) updateFields['membresia.tipo'] = membresia.Tipo;
            if (membresia.tipo !== undefined) updateFields['membresia.tipo'] = membresia.tipo;
            if (membresia.FechaInicio !== undefined) updateFields['membresia.fechaInicio'] = membresia.FechaInicio;
            if (membresia.fechaInicio !== undefined) updateFields['membresia.fechaInicio'] = membresia.fechaInicio;
            if (membresia.FechaFin !== undefined) updateFields['membresia.fechaFin'] = membresia.FechaFin;
            if (membresia.fechaFin !== undefined) updateFields['membresia.fechaFin'] = membresia.fechaFin;
        }

        if (Object.keys(updateFields).length === 0) return res.status(400).json({ message: "No se proporcionaron campos para actualizar" });

        const result = await modelsUsuario.updateOne({ dni }, { $set: updateFields });
        if (result.modifiedCount === 0) return res.status(404).json({ message: "Usuario no encontrado o datos sin cambios" });

        // Si se actualizó la reputación manualmente, sincronizar con los registros existentes
        if (reputacion !== undefined) {
            try {
                const usuario = await modelsUsuario.findOne({ dni });
                const reputacionCalculada = await ReputacionService.calcularReputacionTotal(usuario._id);
                
                // Si hay diferencia entre lo manual y lo calculado, usar el valor manual
                if (reputacionCalculada !== reputacion) {
                    console.log(`⚠️ Reputación manual (${reputacion}) difiere de la calculada (${reputacionCalculada})`);
                }
            } catch (error) {
                console.error('❌ Error verificando reputación:', error.message);
            }
        }

        res.status(200).json({ message: "Usuario actualizado" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// UPDATE USER REPUTATION by dni (WPF/Admin)
router.patch('/updateReputacion', verifyToken, async (req, res) => {
    try {
        const dni = req.body.Dni || req.body.dni;
        const nuevaReputacion = req.body.Reputacion || req.body.reputacion;
        const recalcular = req.body.Recalcular || req.body.recalcular || false;

        if (!dni) return res.status(400).json({ message: "Falta el campo dni" });

        const usuario = await modelsUsuario.findOne({ dni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });

        let reputacionFinal;

        if (recalcular) {
            // Recalcular basándose en los registros de reputación
            reputacionFinal = await ReputacionService.calcularReputacionTotal(usuario._id);
            console.log(`🔄 Reputación recalculada para ${usuario.nombre}: ${reputacionFinal}`);
        } else if (nuevaReputacion !== undefined) {
            // Usar valor manual
            reputacionFinal = nuevaReputacion;
            console.log(`✏️ Reputación manual para ${usuario.nombre}: ${reputacionFinal}`);
        } else {
            return res.status(400).json({ message: "Debe proporcionar nueva reputación o activar recálculo" });
        }

        // Actualizar la reputación
        await modelsUsuario.updateOne({ dni }, { $set: { reputacion: reputacionFinal } });

        // Verificar actualización
        const usuarioActualizado = await modelsUsuario.findOne({ dni });

        res.status(200).json({
            message: "Reputación actualizada",
            reputacionAnterior: usuario.reputacion,
            reputacionNueva: usuarioActualizado.reputacion,
            metodo: recalcular ? 'recalculada' : 'manual'
        });

    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// UPDATE USER DISCOUNT by dni (WPF/Admin) - Descuento por permanencia
router.patch('/updateDescuento', verifyToken, async (req, res) => {
    try {
        const dni = req.body.Dni || req.body.dni;
        const nuevoDescuento = req.body.DescuentoActual !== undefined ? req.body.DescuentoActual : req.body.descuentoActual;
        const motivo = req.body.Motivo || req.body.motivo || 'Actualización manual de descuento';

        if (!dni) return res.status(400).json({ message: "Falta el campo dni" });
        if (nuevoDescuento === undefined || nuevoDescuento === null) {
            return res.status(400).json({ message: "Falta el campo DescuentoActual" });
        }

        // Validar que sea un número entre 0 y 100
        const descuento = parseFloat(nuevoDescuento);
        if (isNaN(descuento) || descuento < 0 || descuento > 100) {
            return res.status(400).json({
                message: "El DescuentoActual debe ser un número entre 0 y 100"
            });
        }

        const usuario = await modelsUsuario.findOne({ dni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });

        const descuentoAnterior = usuario.descuentoActual || 0;

        // Actualizar solo descuentoActual
        const resultado = await modelsUsuario.updateOne(
            { dni },
            { $set: { descuentoActual: descuento } }
        );

        if (resultado.modifiedCount === 0) {
            return res.status(404).json({ message: "Usuario no encontrado o no hubo cambios" });
        }

        console.log(`✏️ Descuento actualizado para ${usuario.nombre}: ${descuentoAnterior}% → ${descuento}%`);

        res.status(200).json({
            message: "Descuento actualizado exitosamente",
            dni: dni,
            descuentoAnterior: descuentoAnterior,
            descuentoNuevo: descuento,
            motivo: motivo,
            fechaCambio: new Date()
        });

    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE USER by dni (WPF)
router.delete('/delete', verifyToken, async (req, res) => {
    try {
        const { dni } = req.body;

        // Verificar permisos: empleados no pueden eliminar a administradores ni empleados
        const currentUserRole = req.user.rol; // Del token JWT
        if (currentUserRole === 'empleado') {
            const targetUser = await modelsUsuario.findOne({ dni });
            if (!targetUser) return res.status(404).json({ message: "Usuario no encontrado" });

            if (targetUser.rol === 'administrador') {
                return res.status(403).json({ message: "No tienes permisos para eliminar un administrador" });
            }
            if (targetUser.rol === 'empleado') {
                return res.status(403).json({ message: "No tienes permisos para eliminar a otros empleados" });
            }
        }

        const result = await modelsUsuario.deleteOne({ dni });
        if (result.deletedCount === 0) return res.status(404).json({ message: "Usuario no encontrado" });
        res.status(200).json({ message: `Usuario con dni ${dni} eliminado` });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// GET ONE USER by email (WPF)
router.post('/getOneEmail', verifyToken, async (req, res) => {
    try {
        const usuario = await modelsUsuario.findOne({ email: req.body.email });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });
        res.status(200).json(usuario);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET ONE USER by dni (WPF)
router.post('/getOneDni', verifyToken, async (req, res) => {
    try {
        const usuario = await modelsUsuario.findOne({ dni: req.body.dni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });
        res.status(200).json(usuario);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// FILTER USERS (WPF)
router.post('/getFilterInter', verifyToken, async (req, res) => {
    try {
        const condiciones = {};
        if (req.body.rol) condiciones.rol = req.body.rol;
        if (req.body.ciudad) condiciones.ciudad = req.body.ciudad;
        if (req.body.telefono) condiciones.telefono = req.body.telefono;
        const usuarios = await modelsUsuario.find(condiciones);
        if (usuarios.length === 0) return res.status(404).json({ message: "No se encontraron usuarios" });
        res.status(200).json(usuarios);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// FILTER USERS (Android)
router.post('/getFilter', verifyToken, async (req, res) => {
    try {
        const condiciones = {};
        if (req.body.dni) condiciones.dni = req.body.dni;
        if (req.body.nombre) condiciones.nombre = req.body.nombre;
        if (req.body.apellidos) condiciones.apellidos = req.body.apellidos;
        if (req.body.email) condiciones.email = req.body.email;
        if (req.body.rol) condiciones.rol = req.body.rol;
        if (req.body.ciudad) condiciones.ciudad = req.body.ciudad;

        const usuarios = await modelsUsuario.find(condiciones);
        if (usuarios.length === 0) return res.status(404).json({ message: "No se encontraron usuarios" });
        res.status(200).json(usuarios);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// REGISTER (Android + Admin mejorado)
router.post('/register', async (req, res) => {
    try {
        const { dni, email, password } = req.body;

        if (!password || password.trim() === '') {
            return res.status(400).json({ message: "La contraseña es requerida" });
        }

        // Validar formato de email
        if (email && !isValidEmail(email)) {
            return res.status(400).json({
                message: "El formato del correo no es válido. Debe usar dominios como @gmail.com, @outlook.es, @yahoo.com, o correos corporativos como @alu.edu.gva.es"
            });
        }

        // Validar ciudad española
        if (req.body.ciudad && !isValidSpanishCity(req.body.ciudad)) {
            return res.status(400).json({
                message: "La ciudad debe ser una ciudad española válida"
            });
        }

        const existing = await modelsUsuario.findOne({ $or: [{ dni }, { email }] });
        if (existing) return res.status(400).json({ message: "Usuario ya existente" });

        // El registro siempre crea clientes (nunca administradores ni empleados)
        const requestedRole = req.body.rol;
        if (requestedRole && (requestedRole.toLowerCase() === 'administrador' || requestedRole.toLowerCase() === 'empleado')) {
            return res.status(403).json({
                message: "No se permite crear " + requestedRole + " mediante registro. Solo se pueden crear clientes."
            });
        }

        const hashedPassword = await bcrypt.hash(password.trim(), 10);
        const usuario = new modelsUsuario({
            dni: req.body.dni,
            nombre: req.body.nombre,
            apellidos: req.body.apellidos,
            email: req.body.email,
            password: hashedPassword,
            rol: "cliente", // ✅ Siempre cliente
            fechaNacimiento: req.body.fechaNacimiento,
            ciudad: req.body.ciudad,
            telefono: req.body.telefono,
            reputacion: req.body.reputacion || 0,
            descuentosActivos: [],
            imagenPerfil: req.body.imagenPerfil
        });
        const savedUser = await usuario.save();
        res.status(200).json(savedUser);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

module.exports = router;
