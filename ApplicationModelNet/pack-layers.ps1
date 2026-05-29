# pack-layers.ps1
# High-speed multi-layered packing script for Promatis.Net AI context (With PDF Subfolder)

$ErrorActionPreference = "Stop"

# 1. Проверка repomix
if (-not (Get-Command repomix -ErrorAction SilentlyContinue)) {
    Write-Host "[!] Repomix not found in system." -ForegroundColor Yellow
    $choice = Read-Host "Install repomix globally via npm? (y/n)"
    if ($choice -eq 'y' -or $choice -eq 'Y') {
        Write-Host "[*] Installing repomix..." -ForegroundColor Cyan
        Start-Process -FilePath "cmd.exe" -ArgumentList "/c npm install -g repomix" -NoNewWindow -Wait
    } else {
        Write-Error "Execution stopped: repomix is required."
    }
}

# 2. Создаем базовую директорию контекста
$outputFolder = "./.context-layers"
if (-not (Test-Path $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
}

# Добавлено: Создаем изолированную подпапку для PDF документов
$pdfFolder = "$outputFolder/pdf"
if (-not (Test-Path $pdfFolder)) {
    New-Item -ItemType Directory -Path $pdfFolder | Out-Null
}

# 3. Установка pdfkit через cmd.exe
if (-not (Test-Path "$outputFolder/node_modules/pdfkit")) {
    Write-Host "[📦] Installing pdfkit library for pure PDF generation..." -ForegroundColor Cyan
    Push-Location $outputFolder
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c npm init -y" -NoNewWindow -Wait
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c npm install pdfkit" -NoNewWindow -Wait
    Pop-Location
}

# 4. Создаем вспомогательный Node.js скрипт для генерации PDF
$nodeScriptPath = "$outputFolder/generate-pdf.js"
$nodeScriptContent = @'
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
'@
Set-Content -Path $nodeScriptPath -Value $nodeScriptContent -Encoding UTF8

# 5. Определение слоев
$layers = @(
    @{ Name = "1_Core";    Path = "Core" },
    @{ Name = "2_MesCore"; Path = "MesCore" },
    @{ Name = "3_MesMDM";  Path = "Mes/MDM" },
    @{ Name = "4_AppMDM";  Path = "App/MDM" }
)

Write-Host "`n[*] Starting Promatis.Net multi-layer extraction...`n" -ForegroundColor Green

# 6. Основной цикл выполнения
foreach ($layer in $layers) {
    $name = $layer.Name
    $targetPath = $layer.Path
    $outputFile = "$outputFolder/$name.txt"
    # Изменено: Путь сохранения файла перенаправлен в подпапку pdf
    $pdfFile = "$pdfFolder/$name.pdf"

    if (-not (Test-Path "./$targetPath")) {
        Write-Host "[>] Skip layer ${name} directory ./$targetPath not found." -ForegroundColor Yellow
        continue
    }

    Write-Host "[>] Packing layer: $name (./$targetPath)..." -ForegroundColor Cyan

    # Сборка XML через repomix
    repomix "./$targetPath" `
            --ignore "**/obj/**,**/bin/**,**/*.png,**/*.jpg,**/node_modules/**,**/*.Designer.cs" `
            --output $outputFile `
            --style xml

    if (Test-Path $outputFile) {
        $size = (Get-Item $outputFile).Length / 1KB
        Write-Host "[+] Layer $name generated. Size KB: " -NoNewline -ForegroundColor Green
        Write-Host ("{0:N2}" -f $size) -ForegroundColor Green

        # 7. Генерация PDF
        Write-Host "[📄] Rendering $name to clean PDF inside subfolder..." -ForegroundColor Magenta
        Start-Process -FilePath "node" -ArgumentList @($nodeScriptPath, $outputFile, $pdfFile, $name) -NoNewWindow -Wait

        if (Test-Path $pdfFile) {
            Write-Host "[✅] PDF for $name successfully saved!" -ForegroundColor Magenta
        } else {
            Write-Host "[-] PDF generation failed via Node.js" -ForegroundColor Red
        }
    } else {
        Write-Host "[-] Failed to generate layer $name" -ForegroundColor Red
    }
}

Write-Host "`n[🎉] SUCCESS! All layers prepared inside: $outputFolder" -ForegroundColor Green
Write-Host "Press enter to exit..."
[void][System.Console]::ReadLine()