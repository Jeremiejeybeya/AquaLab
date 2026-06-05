@echo off
echo.
echo  AquaLab - Systeme de gestion de laboratoire
echo  ==============================================
echo.

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERREUR: .NET SDK non trouve.
    echo Telecharger: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Restauration des packages...
dotnet restore AquaLab\AquaLab.csproj

echo.
echo Demarrage du serveur...
echo Ouvrir : http://localhost:5000
echo.

dotnet run --project AquaLab\AquaLab.csproj --urls="http://localhost:5000"
pause
