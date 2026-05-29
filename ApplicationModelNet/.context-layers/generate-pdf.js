const PDFDocument = require('pdfkit');
const fs = require('fs');

const txtPath = process.argv[2];
const pdfPath = process.argv[3];
const layerName = process.argv[4];

if (!txtPath || !pdfPath) {
    console.error('Missing arguments');
    process.exit(1);
}

const doc = new PDFDocument({ size: 'A4', margin: 40 });
const stream = fs.createWriteStream(pdfPath);
doc.pipe(stream);

doc.font('Courier').fontSize(8.5);
doc.text('LAYER: ' + layerName);
doc.moveDown();

const txtContent = fs.readFileSync(txtPath, 'utf8');
doc.text(txtContent, {
    width: 515,
    align: 'left',
    lineGap: 1
});

doc.end();
stream.on('finish', () => { process.exit(0); });
