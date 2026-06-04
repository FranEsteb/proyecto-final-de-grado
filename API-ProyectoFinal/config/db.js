const mongoose = require('mongoose');
require('dotenv').config();

const connectDB = async () => {
    try {
        await mongoose.connect(process.env.DATABASE_URL);
        console.log('MongoDB Atlas conectado correctamente');
    } catch (error) {
        console.error('Error al conectar MongoDB:', error);
        process.exit(1);
    }
};

module.exports = connectDB;
