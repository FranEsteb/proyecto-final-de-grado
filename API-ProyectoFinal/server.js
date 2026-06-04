require('dotenv').config();
require('./models');
const express = require('express');
const bodyParser = require('body-parser');
const connectDB = require('./config/db');
const https = require('https');
const fs = require('fs');
const path = require('path');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');

const app = express();

// Conexión a MongoDB
connectDB();

// Cabeceras de seguridad HTTP
app.use(helmet({ contentSecurityPolicy: false }));

// Protección contra inyección NoSQL: elimina claves que empiezan por $ o contienen .
function sanitizeObj(obj) {
    if (obj && typeof obj === 'object' && !Array.isArray(obj)) {
        for (const key of Object.keys(obj)) {
            if (key.startsWith('$') || key.includes('.')) {
                delete obj[key];
            } else {
                sanitizeObj(obj[key]);
            }
        }
    }
}
app.use((req, res, next) => {
    if (req.body) sanitizeObj(req.body);
    next();
});

// Middleware CORS
app.use((req, res, next) => {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, PATCH, DELETE, OPTIONS');
    res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept, Authorization');
    if (req.method === 'OPTIONS') {
        res.sendStatus(200);
    } else {
        next();
    }
});

// Rate limiting en rutas de autenticación (máx 10 intentos por 15 minutos por IP)
const authLimiter = rateLimit({
    windowMs: 15 * 60 * 1000,
    max: 10,
    message: { message: 'Demasiados intentos. Inténtalo de nuevo en 15 minutos.' },
    standardHeaders: true,
    legacyHeaders: false,
});
app.use('/api/auth', authLimiter);

// Middleware bodyParser
app.use(bodyParser.json({ limit: '10mb' }));
app.use(bodyParser.urlencoded({ limit: '10mb', extended: true }));

// Importar rutas
const usuarioRoutes = require('./routes/usuarioRoutes');
const authRoutes = require('./routes/authRoutes');
const maquinaRoutes = require('./routes/maquinaRoutes');
const maquinaAdminRoutes = require('./routes/maquinaAdminRoutes');
const averiaRoutes = require('./routes/averiaRoutes');
const proveedorRoutes = require('./routes/proveedorRoutes');
const pedidoRoutes = require('./routes/pedidoRoutes');
const descuentoRoutes = require('./routes/descuentoRoutes');
const claseRoutes = require('./routes/claseRoutes');
const reservaRoutes = require('./routes/reservaRoutes');
const reputacionRoutes = require('./routes/reputacionRoutes');
const membresiaRoutes = require('./routes/membresiaRoutes');
const descuentoCalculatorRoutes = require('./routes/descuentoCalculatorRoutes');
const tecnicoRoutes = require('./routes/tecnicoRoutes');
const costoRoutes = require('./routes/costoRoutes'); 





// Usar rutas
app.use('/api/usuario', usuarioRoutes);
app.use('/api/auth', authRoutes);
app.use('/maquina', maquinaRoutes);
app.use('/maquina/admin', maquinaAdminRoutes);
app.use('/averia', averiaRoutes);
app.use('/proveedor', proveedorRoutes);
app.use('/pedido', pedidoRoutes);
app.use('/descuento', descuentoRoutes); 
app.use('/api/clases', claseRoutes); 
app.use('/api/reservas', reservaRoutes); 
app.use('/reputacion', reputacionRoutes);
app.use('/api/membresia', membresiaRoutes);
app.use('/api/descuentos', descuentoCalculatorRoutes);
app.use('/tecnico', tecnicoRoutes);
app.use('/costo', costoRoutes);




// ── Certificado HTTPS autofirmado (generado una vez, reutilizado) ──────────────
const PORT = process.env.PORT || 3000;

async function startServer() {
    const certsDir = path.join(__dirname, 'certs');
    if (!fs.existsSync(certsDir)) fs.mkdirSync(certsDir);

    const certFile = path.join(certsDir, 'server.pem');
    const keyFile  = path.join(certsDir, 'server.key');

    let certPem, keyPem;
    if (fs.existsSync(certFile) && fs.existsSync(keyFile)) {
        certPem = fs.readFileSync(certFile);
        keyPem  = fs.readFileSync(keyFile);
    } else {
        const selfsigned = require('selfsigned');
        const attrs = [{ name: 'commonName', value: 'localhost' }];
        const pems = await selfsigned.generate(attrs, {
            days: 3650,
            extensions: [{
                name: 'subjectAltName',
                altNames: [
                    { type: 2, value: 'localhost' },
                    { type: 7, ip: '127.0.0.1' },
                    { type: 7, ip: '10.0.2.2' }
                ]
            }]
        });
        fs.writeFileSync(certFile, pems.cert);
        fs.writeFileSync(keyFile, pems.private);
        certPem = pems.cert;
        keyPem  = pems.private;
        console.log('✅ Certificado HTTPS generado en ./certs/');
    }

    https.createServer({ key: keyPem, cert: certPem }, app).listen(PORT, () => {
        console.log(`Servidor HTTPS iniciado en puerto ${PORT}`);
    });
}

startServer();
