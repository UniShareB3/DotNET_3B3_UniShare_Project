const express = require('express');
const path = require('path');
const app = express();

const port = process.env.PORT || 8080;

// Servim fișierele statice din folderul generat de Flutter
app.use(express.static(path.join(__dirname, 'build/web')));

// Pentru orice rută (ex: /login, /home), trimitem index.html
app.get('*', (req, res) => {
    res.sendFile(path.join(__dirname, 'build/web/index.html'));
});

app.listen(port, () => {
    console.log(`Server is running on port ${port}`);
});