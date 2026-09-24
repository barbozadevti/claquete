@echo off
chcp 65001 >nul
title Claquete

rem Se o Claquete ja estiver rodando, so abre o navegador.
powershell -NoProfile -Command "try { Invoke-WebRequest http://localhost:5190/api/saude -UseBasicParsing -TimeoutSec 2 | Out-Null; exit 0 } catch { exit 1 }"
if %errorlevel%==0 (
  start "" http://localhost:5190
  exit /b
)

cd /d "%~dp0src\Claquete.Api"
echo.
echo   Claquete - iniciando o servidor...
echo   O navegador abre sozinho quando estiver pronto.
echo   Para encerrar, feche esta janela.
echo.
dotnet run --no-launch-profile -- --urls http://localhost:5190 --AbrirNavegador=true
if errorlevel 1 pause
