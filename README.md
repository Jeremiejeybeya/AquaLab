# 💧 AquaLab — Gestion de Laboratoire d'Eau Usée
### Application web complète pour ONG de traitement des eaux

---

## 📋 Description

AquaLab est une application web ASP.NET Core 8 conçue pour les laboratoires de traitement d'eaux usées. Elle permet :

- 🧪 **Saisie des prélèvements** avec tous les paramètres physico-chimiques
- ⚗️ **Aide à la décision** : calcul automatique des doses de produits (chlore, coagulant, floquant, chaux)
- 🔔 **Alertes automatiques** quand des paramètres dépassent les normes OMS
- 📊 **Tableau de bord** en temps réel
- 📈 **Rapports & historique** des 30 derniers jours
- 👤 **Gestion des techniciens** et des sites de prélèvement

---

## 🚀 Installation & Démarrage

### Prérequis
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Étapes

```bash
# 1. Cloner / copier le projet
cd AquaLab

# 2. Restaurer les packages
dotnet restore AquaLab/AquaLab.csproj

# 3. Lancer l'application
dotnet run --project AquaLab/AquaLab.csproj

# 4. Ouvrir dans le navigateur
# http://localhost:5000
```

La base de données SQLite (`aqualab.db`) est créée automatiquement au premier démarrage avec des données de démonstration.

---

## 🗂️ Structure du projet

```
AquaLab/
├── AquaLab.sln
└── AquaLab/
    ├── AquaLab.csproj          # Dépendances NuGet
    ├── Program.cs               # Démarrage, injection de dépendances
    ├── Models/
    │   └── Models.cs            # Technicien, Prelevement, Traitement, Alerte…
    ├── Data/
    │   └── AquaLabContext.cs    # Entity Framework + données initiales
    ├── Controllers/
    │   └── Controllers.cs       # API REST : /api/prelevements, /api/traitements…
    ├── Services/
    │   ├── DecisionTraitementService.cs  # 🧠 Moteur d'aide à la décision
    │   └── AlerteService.cs              # 🔔 Génération automatique d'alertes
    └── wwwroot/
        └── index.html           # Frontend SPA (HTML + CSS + JS)
```

---

## 🧠 Moteur d'aide à la décision

Le service `DecisionTraitementService` calcule automatiquement :

| Paramètre | Norme | Action déclenchée |
|-----------|-------|-------------------|
| pH < 6.5  | 6.5–8.5 | Dose de chaux Ca(OH)₂ |
| pH > 8.5  | 6.5–8.5 | Dose d'acide HCl |
| Turbidité > 5 NTU | < 5 NTU | Coagulant + Floquant |
| Turbidité > 100 NTU | — | TRAITEMENT URGENCE |
| DCO > 125 mg/L | < 125 mg/L | Traitement biologique renforcé |
| Coliformes > 1000 UFC | < 1000 UFC | Dose chlore augmentée |
| Phosphore > 2 mg/L | < 2 mg/L | Précipitation FeCl₃ |
| Ammonium > 10 mg/L | < 10 mg/L | Vérification nitrification |
| O₂ < 2 mg/L | > 2 mg/L | Alerte critique aération |

### Formule chlore
```
Dose totale = Dose base + Demande organique (DCO×0.005) + Demande ammonium (NH4×7.6)
```

### Score qualité (0–100)
Pénalités automatiques selon l'écart aux normes, pondérées par paramètre :
- pH : 20 pts · Turbidité : 15 pts · DCO : 20 pts · DBO5 : 15 pts
- MES : 10 pts · Coliformes : 15 pts · Ammonium : 10 pts · Phosphore : 5 pts

---

## 🔌 API REST

| Méthode | Endpoint | Description |
|---------|----------|-------------|
| GET | `/api/prelevements` | Liste paginée |
| POST | `/api/prelevements` | Nouveau prélèvement (génère alertes auto) |
| GET | `/api/prelevements/{id}` | Détail d'un prélèvement |
| GET | `/api/traitements/recommandation/{id}` | **Calcul aide à la décision** |
| POST | `/api/traitements` | Enregistrer un traitement |
| PUT | `/api/traitements/{id}/appliquer` | Confirmer l'application + résultats |
| GET | `/api/alertes?nonAcquittees=true` | Alertes actives |
| PUT | `/api/alertes/{id}/acquitter` | Acquitter une alerte |
| GET | `/api/rapports/dashboard` | Stats tableau de bord |
| GET | `/api/rapports/evolution?jours=30` | Données graphiques |
| GET/POST | `/api/techniciens` | Gestion techniciens |
| GET/POST | `/api/pointsprelevement` | Gestion sites |

---

## 📊 Paramètres mesurés

- **Physiques** : pH, Turbidité (NTU), Température (°C), Conductivité (µS/cm)
- **Oxygène** : O₂ Dissous (mg/L)
- **Organiques** : DCO (mg/L), DBO5 (mg/L), MES (mg/L)
- **Azote** : Nitrates NO₃, Nitrites NO₂, Ammonium NH₄ (mg/L)
- **Autres** : Phosphore total, Coliformes fécaux (UFC/100mL), Chlore résiduel

---

## 🔧 Extensions possibles

- [ ] Authentification JWT (rôles Technicien / Chef de Labo)
- [ ] Export PDF/Excel des rapports
- [ ] Graphiques interactifs (Chart.js / Recharts)
- [ ] Notifications email lors d'alertes critiques
- [ ] Application mobile (MAUI)
- [ ] Intégration capteurs IoT (MQTT)
- [ ] Migration vers PostgreSQL pour multi-utilisateurs réseau
- [ ] QR codes sur les flacons de prélèvement

---

## 📞 Support

Développé pour ONG de traitement des eaux usées.
Architecture : **ASP.NET Core 8** + **Entity Framework Core** + **SQLite** + **SPA vanilla JS**
