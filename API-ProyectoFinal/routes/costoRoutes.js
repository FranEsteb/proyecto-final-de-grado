const express = require('express');
const modelsCosto = require('../models/modelsCosto');
const verifyToken = require('../middlewares/authMiddleware');

const router = express.Router();

// GET ALL: Obtener todos los costos
router.get('/getAll', verifyToken, async (req, res) => {
    try {
        const costos = await modelsCosto.find()
            .populate('maquina', 'numeroSerie tipo marca modelo')
            .populate('averia', 'descripcion')
            .populate('tecnico', 'nombre apellidos')
            .populate('proveedor', 'nombre')
            .sort({ fecha: -1 });

        res.status(200).json(costos);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET BY FILTROS: Obtener costos con filtros
router.post('/filtrar', verifyToken, async (req, res) => {
    try {
        const filtros = {};

        // Filtro por rango de fechas
        if (req.body.fechaDesde || req.body.fechaHasta) {
            filtros.fecha = {};
            if (req.body.fechaDesde) {
                filtros.fecha.$gte = new Date(req.body.fechaDesde);
            }
            if (req.body.fechaHasta) {
                filtros.fecha.$lte = new Date(req.body.fechaHasta);
            }
        }

        // Filtro por tipo de costo
        if (req.body.tipoCosto) {
            filtros.tipoCosto = req.body.tipoCosto;
        }

        // Filtro por máquina
        if (req.body.maquinaId) {
            filtros.maquina = req.body.maquinaId;
        }

        // Filtro por técnico
        if (req.body.tecnicoId) {
            filtros.tecnico = req.body.tecnicoId;
        }

        // Filtro por estado de pago
        if (req.body.estadoPago) {
            filtros.estadoPago = req.body.estadoPago;
        }

        // Filtro por rango de montos
        if (req.body.montoMinimo || req.body.montoMaximo) {
            filtros.monto = {};
            if (req.body.montoMinimo) {
                filtros.monto.$gte = parseFloat(req.body.montoMinimo);
            }
            if (req.body.montoMaximo) {
                filtros.monto.$lte = parseFloat(req.body.montoMaximo);
            }
        }

        const costos = await modelsCosto.find(filtros)
            .populate('maquina', 'numeroSerie tipo marca modelo')
            .populate('averia', 'descripcion')
            .populate('tecnico', 'nombre apellidos')
            .populate('proveedor', 'nombre')
            .sort({ fecha: -1 });

        res.status(200).json(costos);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET BY ID: Obtener costo por ID
router.get('/:id', verifyToken, async (req, res) => {
    try {
        const costo = await modelsCosto.findById(req.params.id)
            .populate('maquina', 'numeroSerie tipo marca modelo')
            .populate('averia', 'descripcion prioridad')
            .populate('tecnico', 'nombre apellidos email telefono')
            .populate('proveedor', 'nombre email telefono');

        if (!costo) {
            return res.status(404).json({ message: "Costo no encontrado" });
        }

        res.status(200).json(costo);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET ESTADISTICAS: Obtener estadísticas de costos
router.post('/estadisticas', verifyToken, async (req, res) => {
    try {
        const filtros = {
            fechaDesde: req.body.fechaDesde,
            fechaHasta: req.body.fechaHasta,
            tipoCosto: req.body.tipoCosto,
            maquina: req.body.maquinaId
        };

        const estadisticas = await modelsCosto.obtenerEstadisticas(filtros);
        res.status(200).json(estadisticas);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET COSTOS POR MAQUINA: Obtener todos los costos de una máquina específica
router.get('/maquina/:maquinaId', verifyToken, async (req, res) => {
    try {
        const { maquinaId } = req.params;
        const { fechaDesde, fechaHasta } = req.query;

        const resultado = await modelsCosto.costosPorMaquina(
            maquinaId,
            fechaDesde,
            fechaHasta
        );

        res.status(200).json(resultado);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// POST NEW: Crear nuevo costo
router.post('/new', verifyToken, async (req, res) => {
    try {
        const nuevoCosto = new modelsCosto({
            tipoCosto: req.body.tipoCosto || req.body.TipoCosto,
            monto: req.body.monto || req.body.Monto,
            fecha: req.body.fecha || req.body.Fecha || new Date(),
            descripcion: req.body.descripcion || req.body.Descripcion,
            maquina: req.body.maquinaId || req.body.MaquinaId,
            averia: req.body.averiaId || req.body.AveriaId,
            tecnico: req.body.tecnicoId || req.body.TecnicoId,
            proveedor: req.body.proveedorId || req.body.ProveedorId,
            numeroFactura: req.body.numeroFactura || req.body.NumeroFactura,
            observaciones: req.body.observaciones || req.body.Observaciones,
            repuestos: req.body.repuestos || req.body.Repuestos,
            usuarioRegistro: req.body.usuarioRegistro || req.user?.email || 'Sistema',
            estadoPago: req.body.estadoPago || req.body.EstadoPago || 'Pendiente'
        });

        const savedCosto = await nuevoCosto.save();

        // Poblar los datos antes de retornar
        const costoCompleto = await modelsCosto.findById(savedCosto._id)
            .populate('maquina', 'numeroSerie tipo')
            .populate('tecnico', 'nombre apellidos')
            .populate('proveedor', 'nombre');

        res.status(201).json(costoCompleto);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// PATCH UPDATE: Actualizar costo
router.patch('/update/:id', verifyToken, async (req, res) => {
    try {
        const updateData = {
            tipoCosto: req.body.tipoCosto || req.body.TipoCosto,
            monto: req.body.monto || req.body.Monto,
            fecha: req.body.fecha || req.body.Fecha,
            descripcion: req.body.descripcion || req.body.Descripcion,
            maquina: req.body.maquinaId || req.body.MaquinaId,
            averia: req.body.averiaId || req.body.AveriaId,
            tecnico: req.body.tecnicoId || req.body.TecnicoId,
            proveedor: req.body.proveedorId || req.body.ProveedorId,
            numeroFactura: req.body.numeroFactura || req.body.NumeroFactura,
            observaciones: req.body.observaciones || req.body.Observaciones,
            repuestos: req.body.repuestos || req.body.Repuestos,
            estadoPago: req.body.estadoPago || req.body.EstadoPago
        };

        // Eliminar campos undefined
        Object.keys(updateData).forEach(key =>
            updateData[key] === undefined && delete updateData[key]
        );

        const costo = await modelsCosto.findByIdAndUpdate(
            req.params.id,
            { $set: updateData },
            { new: true, runValidators: true }
        )
            .populate('maquina', 'numeroSerie tipo')
            .populate('tecnico', 'nombre apellidos')
            .populate('proveedor', 'nombre');

        if (!costo) {
            return res.status(404).json({ message: "Costo no encontrado" });
        }

        res.status(200).json(costo);
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// PATCH APROBAR: Aprobar un costo
router.patch('/aprobar/:id', verifyToken, async (req, res) => {
    try {
        const costo = await modelsCosto.findByIdAndUpdate(
            req.params.id,
            {
                $set: {
                    aprobado: true,
                    aprobadoPor: req.body.aprobadoPor || req.user?.email || 'Administrador',
                    fechaAprobacion: new Date()
                }
            },
            { new: true }
        );

        if (!costo) {
            return res.status(404).json({ message: "Costo no encontrado" });
        }

        res.status(200).json({ message: "Costo aprobado correctamente", costo });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// PATCH MARCAR PAGADO: Marcar costo como pagado
router.patch('/marcarPagado/:id', verifyToken, async (req, res) => {
    try {
        const costo = await modelsCosto.findByIdAndUpdate(
            req.params.id,
            {
                $set: {
                    estadoPago: 'Pagado',
                    fechaPago: new Date()
                }
            },
            { new: true }
        );

        if (!costo) {
            return res.status(404).json({ message: "Costo no encontrado" });
        }

        res.status(200).json({ message: "Costo marcado como pagado", costo });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// DELETE: Eliminar costo
router.delete('/delete/:id', verifyToken, async (req, res) => {
    try {
        const costo = await modelsCosto.findByIdAndDelete(req.params.id);

        if (!costo) {
            return res.status(404).json({ message: "Costo no encontrado" });
        }

        res.status(200).json({ message: "Costo eliminado correctamente" });
    } catch (error) {
        res.status(400).json({ message: error.message });
    }
});

// GET RESUMEN MENSUAL: Obtener resumen de costos por mes
router.get('/resumen/mensual', verifyToken, async (req, res) => {
    try {
        const { anio } = req.query;
        const year = parseInt(anio) || new Date().getFullYear();

        const costosMensuales = await modelsCosto.aggregate([
            {
                $match: {
                    fecha: {
                        $gte: new Date(`${year}-01-01`),
                        $lte: new Date(`${year}-12-31`)
                    }
                }
            },
            {
                $group: {
                    _id: { $month: '$fecha' },
                    total: { $sum: '$monto' },
                    cantidad: { $sum: 1 }
                }
            },
            {
                $sort: { _id: 1 }
            }
        ]);

        res.status(200).json(costosMensuales);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

// GET PENDIENTES APROBACION: Obtener costos pendientes de aprobación
router.get('/pendientes/aprobacion', verifyToken, async (req, res) => {
    try {
        const costosPendientes = await modelsCosto.find({
            requiereAprobacion: true,
            aprobado: false
        })
            .populate('maquina', 'numeroSerie tipo')
            .populate('tecnico', 'nombre apellidos')
            .sort({ fecha: -1 });

        res.status(200).json(costosPendientes);
    } catch (error) {
        res.status(500).json({ message: error.message });
    }
});

module.exports = router;
