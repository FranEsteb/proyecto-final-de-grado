const express = require('express');
const modelsReserva = require('../models/modelsReserva');
const modelsUsuario = require('../models/modelsUsuario');  // IMPORTANTE
const modelsClase = require('../models/modelsClase');      // IMPORTANTE
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// GET ALL
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const reservas = await modelsReserva.find().populate('usuario').populate('clase');
        res.status(200).json(reservas);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// CREATE NEW
router.post('/new', verifyToken, async (req, res) => {
    try {
        const { idReserva, usuarioDni, idClase, estado, observaciones } = req.body;

        // Buscar el usuario por su DNI
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado con ese DNI" });

        // Buscar la clase por su idClase
        const clase = await modelsClase.findOne({ idClase: idClase });
        if (!clase) return res.status(404).json({ message: "Clase no encontrada con ese ID" });

        // Crear la nueva reserva
        const nuevaReserva = new modelsReserva({
            idReserva: idReserva,
            usuario: usuario._id,
            clase: clase._id,
            estado: estado,
            observaciones: observaciones
        });

        const savedReserva = await nuevaReserva.save();
        res.status(200).json(savedReserva);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// UPDATE by idReserva
router.patch('/update', verifyToken, async (req, res) => {
    try {
        const { idReserva } = req.body;
        if (!idReserva) return res.status(400).json({ message: "Falta el campo idReserva" });

        const updateFields = {};

        if (req.body.usuario !== undefined) updateFields.usuario = req.body.usuario;
        if (req.body.clase !== undefined) updateFields.clase = req.body.clase;
        if (req.body.estado !== undefined) updateFields.estado = req.body.estado;
        if (req.body.observaciones !== undefined) updateFields.observaciones = req.body.observaciones;

        const result = await modelsReserva.updateOne({ idReserva }, { $set: updateFields });
        if (result.modifiedCount === 0) return res.status(404).json({ message: "Reserva no encontrada o datos sin cambios" });

        res.status(200).json({ message: "Reserva actualizada" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE by idReserva
router.delete('/delete', verifyToken, async (req, res) => {
    try {
        const { idReserva } = req.body;
        const result = await modelsReserva.deleteOne({ idReserva });
        if (result.deletedCount === 0) return res.status(404).json({ message: "Reserva no encontrada" });

        res.status(200).json({ message: `Reserva con ID ${idReserva} eliminada` });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// GET ONE by idReserva
router.post('/getOne', verifyToken, async (req, res) => {
    try {
        const reserva = await modelsReserva.findOne({ idReserva: req.body.idReserva }).populate('usuario').populate('clase');
        if (!reserva) return res.status(404).json({ message: "Reserva no encontrada" });

        res.status(200).json(reserva);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET MIS RESERVAS (obtiene las reservas activas del usuario autenticado)
router.get('/mis-reservas', verifyToken, async (req, res) => {
    try {
        const usuarioDni = req.user.dni;

        if (!usuarioDni) return res.status(400).json({ message: "DNI no encontrado en el token" });

        // Buscar el usuario por su DNI
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });

        // Buscar todas las reservas activas del usuario y popular los datos de la clase
        const reservas = await modelsReserva.find({
            usuario: usuario._id,
            estado: 'activa'
        }).populate('clase');

        // Formatear la respuesta
        const reservasFormateadas = reservas.map(reserva => ({
            idReserva: reserva.idReserva,
            fechaReserva: reserva.fechaReserva,
            estado: reserva.estado,
            clase: reserva.clase ? {
                idClase: reserva.clase.idClase,
                nombre: reserva.clase.nombre,
                descripcion: reserva.clase.descripcion,
                instructor: reserva.clase.instructor,
                fechaHora: reserva.clase.fechaHora,
                duracionMinutos: reserva.clase.duracionMinutos,
                capacidadMaxima: reserva.clase.capacidadMaxima,
                inscritosCount: reserva.clase.inscritos ? reserva.clase.inscritos.length : 0,
                sala: reserva.clase.sala,
                estado: reserva.clase.estado
            } : null
        }));

        console.log(`Usuario ${usuarioDni} tiene ${reservasFormateadas.length} reservas activas`);
        res.status(200).json(reservasFormateadas);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// RESERVAR CLASE (endpoint simplificado que usa el DNI del token)
router.post('/reservar', verifyToken, async (req, res) => {
    try {
        const { idClase } = req.body;

        // Obtener DNI del token (viene del middleware verifyToken en req.user)
        const usuarioDni = req.user.dni;

        if (!usuarioDni) return res.status(400).json({ message: "DNI no encontrado en el token" });
        if (!idClase) return res.status(400).json({ message: "idClase es requerido" });

        // Buscar el usuario por su DNI
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });

        // Buscar la clase por su idClase
        const clase = await modelsClase.findOne({ idClase: idClase });
        if (!clase) return res.status(404).json({ message: "Clase no encontrada" });

        // Verificar si la clase está llena
        if (clase.inscritosCount >= clase.capacidadMaxima) {
            return res.status(400).json({ message: "La clase está completa" });
        }

        // Verificar si ya existe una reserva para este usuario y clase
        const reservaExistente = await modelsReserva.findOne({
            usuario: usuario._id,
            clase: clase._id,
            estado: 'activa'
        });

        if (reservaExistente) {
            return res.status(400).json({ message: "Ya tienes una reserva activa para esta clase" });
        }

        // Generar ID único para la reserva
        const idReserva = `RES-${Date.now()}-${usuario._id.toString().slice(-4)}`;

        // Crear la nueva reserva
        const nuevaReserva = new modelsReserva({
            idReserva: idReserva,
            usuario: usuario._id,
            clase: clase._id,
            estado: 'activa'
        });

        const savedReserva = await nuevaReserva.save();

        // Agregar usuario a la lista de inscritos de la clase
        await modelsClase.updateOne(
            { _id: clase._id },
            { $addToSet: { inscritos: usuario._id } }
        );

        res.status(200).json({
            message: "Reserva creada exitosamente",
            reserva: savedReserva
        });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// CANCELAR RESERVA (endpoint para que el cliente cancele su propia reserva)
router.post('/cancelar', verifyToken, async (req, res) => {
    try {
        const { idReserva } = req.body;

        // Obtener DNI del token
        const usuarioDni = req.user.dni;

        if (!usuarioDni) return res.status(400).json({ message: "DNI no encontrado en el token" });
        if (!idReserva) return res.status(400).json({ message: "idReserva es requerido" });

        // Buscar el usuario por su DNI
        const usuario = await modelsUsuario.findOne({ dni: usuarioDni });
        if (!usuario) return res.status(404).json({ message: "Usuario no encontrado" });

        // Buscar la reserva
        const reserva = await modelsReserva.findOne({ idReserva: idReserva });
        if (!reserva) return res.status(404).json({ message: "Reserva no encontrada" });

        // Verificar que la reserva pertenece al usuario
        if (reserva.usuario.toString() !== usuario._id.toString()) {
            return res.status(403).json({ message: "No tienes permiso para cancelar esta reserva" });
        }

        // Verificar que la reserva está activa
        if (reserva.estado !== 'activa') {
            return res.status(400).json({ message: "Solo se pueden cancelar reservas activas" });
        }

        // Cambiar el estado de la reserva a 'cancelada'
        reserva.estado = 'cancelada';
        await reserva.save();

        // Eliminar usuario de la lista de inscritos de la clase
        await modelsClase.updateOne(
            { _id: reserva.clase },
            { $pull: { inscritos: usuario._id } }
        );

        res.status(200).json({
            message: "Reserva cancelada exitosamente"
        });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

module.exports = router;
