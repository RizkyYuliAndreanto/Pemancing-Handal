@echo off
REM ============================================================
REM  deploy-webgl.bat — Copy Unity WebGL build to docs/ and push
REM  Usage: jalankan dari root project (folder mancingcuy)
REM ============================================================

echo.
echo === MancingCuy WebGL Deploy ===
echo.

REM Check if WebGL build exists
if not exist "WebGLBuild\index.html" (
    echo [ERROR] WebGL build not found!
    echo.
    echo Lakukan build dulu di Unity:
    echo   1. File ^> Build Settings
    echo   2. Platform: WebGL ^> Switch Platform
    echo   3. Set Build folder: WebGLBuild
    echo   4. Klik Build
    echo.
    pause
    exit /b 1
)

echo [1/4] Cleaning docs folder...
if exist "docs" rmdir /s /q "docs"
mkdir "docs"

echo [2/4] Copying WebGL build to docs...
xcopy "WebGLBuild\*" "docs\" /E /I /Q /Y >nul

echo [3/4] Staging changes...
git add docs/
git add .github/

echo [4/4] Committing and pushing...
git commit -m "Deploy WebGL build to GitHub Pages"
git push origin main

echo.
echo === Deploy complete! ===
echo Game will be live at:
echo   https://rizkyyuliandreanto.github.io/Pemancing-Handal/
echo.
echo (Tunggu 1-2 menit untuk GitHub Actions selesai)
pause
