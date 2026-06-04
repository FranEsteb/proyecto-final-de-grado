const validarRegistroMaquina = (req, res, next) => {
    const { numeroSerie, tipo } = req.body;
    const errores = [];

    if (!numeroSerie || numeroSerie.trim().length === 0) {
        errores.push('NumeroSerie es requerido y no puede estar vacío');
    } else if (numeroSerie.length > 50) {
        errores.push('NumeroSerie no puede exceder 50 caracteres');
    }

    if (!tipo || tipo.trim().length === 0) {
        errores.push('Tipo es requerido y no puede estar vacío');
    } else if (tipo.length > 100) {
        errores.push('Tipo no puede exceder 100 caracteres');
    }

    const tiposPermitidos = ['bicicleta', 'cinta', 'elíptica', 'pesas', 'remo', 'escaladora', 'prensa'];
    if (tipo && !tiposPermitidos.includes(tipo.toLowerCase())) {
        errores.push(`Tipo debe ser uno de: ${tiposPermitidos.join(', ')}`);
    }

    if (req.body.marca && req.body.marca.length > 100) {
        errores.push('Marca no puede exceder 100 caracteres');
    }

    if (req.body.modelo && req.body.modelo.length > 100) {
        errores.push('Modelo no puede exceder 100 caracteres');
    }

    if (req.body.ubicacion && req.body.ubicacion.length > 200) {
        errores.push('Ubicacion no puede exceder 200 caracteres');
    }

    if (req.body.fechaCompra && !Date.parse(req.body.fechaCompra)) {
        errores.push('FechaCompra debe ser una fecha válida');
    }

    if (req.body.fechaCompra && new Date(req.body.fechaCompra) > new Date()) {
        errores.push('FechaCompra no puede ser una fecha futura');
    }

    if (req.body.costoCompra && (isNaN(req.body.costoCompra) || req.body.costoCompra < 0)) {
        errores.push('CostoCompra debe ser un número positivo');
    }

    if (req.body.horasUso && (isNaN(req.body.horasUso) || req.body.horasUso < 0)) {
        errores.push('HorasUso debe ser un número positivo');
    }

    if (req.body.estado) {
        const estadosPermitidos = ['operativa', 'en reparación', 'fuera de servicio', 'mantenimiento'];
        if (!estadosPermitidos.includes(req.body.estado)) {
            errores.push(`Estado debe ser uno de: ${estadosPermitidos.join(', ')}`);
        }
    }

    if (errores.length > 0) {
        return res.status(400).json({
            message: 'Errores de validación',
            errores
        });
    }

    req.body.numeroSerie = numeroSerie.trim();
    req.body.tipo = tipo.trim();
    if (req.body.marca) req.body.marca = req.body.marca.trim();
    if (req.body.modelo) req.body.modelo = req.body.modelo.trim();
    if (req.body.ubicacion) req.body.ubicacion = req.body.ubicacion.trim();

    next();
};

const validarActualizacionEstado = (req, res, next) => {
    const { numeroSerie, estado } = req.body;
    const errores = [];

    if (!numeroSerie || numeroSerie.trim().length === 0) {
        errores.push('NumeroSerie es requerido');
    }

    if (!estado || estado.trim().length === 0) {
        errores.push('Estado es requerido');
    } else {
        const estadosPermitidos = ['operativa', 'en reparación', 'fuera de servicio', 'mantenimiento'];
        if (!estadosPermitidos.includes(estado)) {
            errores.push(`Estado debe ser uno de: ${estadosPermitidos.join(', ')}`);
        }
    }

    if (req.body.motivo && req.body.motivo.length > 500) {
        errores.push('Motivo no puede exceder 500 caracteres');
    }

    if (errores.length > 0) {
        return res.status(400).json({
            message: 'Errores de validación',
            errores
        });
    }

    req.body.numeroSerie = numeroSerie.trim();
    req.body.estado = estado.trim();
    if (req.body.motivo) req.body.motivo = req.body.motivo.trim();

    next();
};

const validarHorasUso = (req, res, next) => {
    const { numeroSerie, horasUso } = req.body;
    const errores = [];

    if (!numeroSerie || numeroSerie.trim().length === 0) {
        errores.push('NumeroSerie es requerido');
    }

    if (!horasUso || isNaN(horasUso) || horasUso <= 0) {
        errores.push('HorasUso debe ser un número positivo mayor a 0');
    }

    if (horasUso > 24) {
        errores.push('HorasUso no puede exceder 24 horas por registro');
    }

    if (errores.length > 0) {
        return res.status(400).json({
            message: 'Errores de validación',
            errores
        });
    }

    req.body.numeroSerie = numeroSerie.trim();
    req.body.horasUso = parseFloat(horasUso);

    next();
};

const validarProgramacionMantenimiento = (req, res, next) => {
    const { numeroSerie, fechaMantenimiento } = req.body;
    const errores = [];

    if (!numeroSerie || numeroSerie.trim().length === 0) {
        errores.push('NumeroSerie es requerido');
    }

    if (!fechaMantenimiento || !Date.parse(fechaMantenimiento)) {
        errores.push('FechaMantenimiento debe ser una fecha válida');
    }

    if (fechaMantenimiento && new Date(fechaMantenimiento) < new Date()) {
        errores.push('FechaMantenimiento no puede ser una fecha pasada');
    }

    if (errores.length > 0) {
        return res.status(400).json({
            message: 'Errores de validación',
            errores
        });
    }

    req.body.numeroSerie = numeroSerie.trim();

    next();
};

const sanitizarBusqueda = (req, res, next) => {
    if (req.body.tipo) req.body.tipo = req.body.tipo.toString().trim();
    if (req.body.marca) req.body.marca = req.body.marca.toString().trim();
    if (req.body.modelo) req.body.modelo = req.body.modelo.toString().trim();
    if (req.body.ubicacion) req.body.ubicacion = req.body.ubicacion.toString().trim();
    if (req.body.estado) req.body.estado = req.body.estado.toString().trim();

    if (req.body.estado) {
        const estadosPermitidos = ['operativa', 'en reparación', 'fuera de servicio', 'mantenimiento'];
        if (!estadosPermitidos.includes(req.body.estado)) {
            return res.status(400).json({
                message: 'Estado inválido',
                estadosPermitidos
            });
        }
    }

    next();
};

const limitarResultados = (req, res, next) => {
    const limite = parseInt(req.query.limite) || 50;
    const pagina = parseInt(req.query.pagina) || 1;

    if (limite > 100) {
        return res.status(400).json({
            message: 'El límite máximo es 100 registros por página'
        });
    }

    req.paginacion = {
        limite: Math.max(1, limite),
        saltar: Math.max(0, (pagina - 1) * limite)
    };

    next();
};

const validarObtenerHistorial = (req, res, next) => {
    const { numeroSerie } = req.body;
    const errores = [];

    if (!numeroSerie || typeof numeroSerie !== 'string' || numeroSerie.trim().length === 0) {
        errores.push('NumeroSerie es requerido y debe ser un texto válido');
    } else if (numeroSerie.length > 50) {
        errores.push('NumeroSerie no puede exceder 50 caracteres');
    }

    if (errores.length > 0) {
        return res.status(400).json({
            message: 'Errores de validación',
            errores
        });
    }

    req.body.numeroSerie = numeroSerie.trim();
    next();
};

module.exports = {
    validarRegistroMaquina,
    validarActualizacionEstado,
    validarHorasUso,
    validarProgramacionMantenimiento,
    sanitizarBusqueda,
    limitarResultados,
    validarObtenerHistorial
};