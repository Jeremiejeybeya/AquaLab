#!/bin/bash
# ═══════════════════════════════════════════════
#  AquaLab — Script de démarrage
# ═══════════════════════════════════════════════

echo ""
echo "💧 AquaLab — Système de gestion de laboratoire"
echo "================================================"
echo ""

# Vérifier .NET
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK non trouvé."
    echo "   → Télécharger : https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET SDK détecté : $DOTNET_VERSION"

# Restaurer les packages
echo ""
echo "📦 Restauration des packages NuGet..."
dotnet restore AquaLab/AquaLab.csproj

# Lancer
echo ""
echo "🚀 Démarrage du serveur..."
echo "   → Ouvrir : http://localhost:5000"
echo ""
dotnet run --project AquaLab/AquaLab.csproj --urls="http://localhost:5000"
