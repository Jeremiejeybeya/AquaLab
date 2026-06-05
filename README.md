# 💧 AquaLab — v2.0

Application web ASP.NET Core 8 pour la gestion d'un laboratoire de traitement des eaux usées.

## 🆕 Nouveautés v2.0
- 🔐 Authentification JWT (login/mot de passe)
- 📄 Export PDF des rapports (prélèvement individuel + rapport mensuel)
- 📧 Notifications email pour les alertes critiques (MailKit)
- 👤 Gestion des utilisateurs (Admin, Chef de Labo, Technicien)

## 🚀 Démarrage rapide

```bash
dotnet restore AquaLab/AquaLab.csproj
dotnet run --project AquaLab/AquaLab.csproj --urls="http://localhost:5000"
```

**Compte par défaut : `admin` / `Admin@2024`**

## ⚙️ Configuration email (optionnel)

Dans `appsettings.json` :
```json
"Email": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "Username": "votre@gmail.com",
  "Password": "votre-mot-de-passe-app",
  "From": "aqualab@votre-ong.org",
  "AdminTo": "admin@votre-ong.org"
}
```

Pour Gmail : activez la vérification en 2 étapes puis créez un "Mot de passe d'application".

## 📋 API REST

| Endpoint | Description |
|----------|-------------|
| POST `/api/auth/login` | Connexion → retourne JWT token |
| GET `/api/auth/me` | Profil utilisateur connecté |
| PUT `/api/auth/password` | Changer mot de passe |
| GET `/api/pdf/prelevement/{id}` | Télécharger PDF prélèvement |
| GET `/api/pdf/rapport-mensuel` | Télécharger rapport mensuel PDF |
| POST `/api/auth/register` | Créer compte (Admin seulement) |

