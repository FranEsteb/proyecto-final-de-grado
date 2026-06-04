const mongoose = require('mongoose');

const proveedorSchema = new mongoose.Schema({
    nombre: { type: String, required: true },
    cif: { type: String, required: true, unique: true },    // CIF o NIF de empresa
    direccion: { type: String },
    telefono: { type: String },
    email: { type: String },
    productosSuministrados: [{ type: String }],             // Lista de productos o tipos de repuestos
    observaciones: { type: String }
}, {
    timestamps: true
});

module.exports = mongoose.model('modelsProveedor', proveedorSchema, "proveedores");
