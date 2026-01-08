# MediAid - Plateforme Web d'Assistance Médicale Communautaire

## 📋 Description

MediAid est une plateforme web sécurisée basée sur ASP.NET Core MVC (.NET 8) permettant la mise en relation entre patients et aidants pour une assistance médicale non urgente, sous supervision d'experts et d'administrateurs.

## 🏗️ Architecture

- **Backend**: ASP.NET Core MVC (.NET 8)
- **Base de données**: MongoDB (local)
- **Authentification**: Cookie-based avec support JWT
- **Temps réel**: SignalR pour le chat
- **Frontend**: Razor Views + Bootstrap 5

## 📁 Structure du Projet

```
server/
├── Controllers/       # Contrôleurs MVC
├── Data/             # Contexte MongoDB
├── DTOs/             # Data Transfer Objects
├── Hubs/             # SignalR Hubs
├── Models/           # Modèles de domaine
├── Services/         # Services métier
├── Views/            # Vues Razor
└── wwwroot/          # Fichiers statiques (CSS, JS)
```

## 🚀 Prérequis

- .NET 8 SDK
- MongoDB (local ou distant)
- Visual Studio 2022 ou VS Code

## ⚙️ Configuration

1. **MongoDB**: Assurez-vous que MongoDB est installé et fonctionne sur `mongodb://localhost:27017`

2. **Configuration**: Modifiez `appsettings.json` si nécessaire:
   ```json
   {
     "ConnectionStrings": {
       "MongoDB": "mongodb://localhost:27017",
       "DatabaseName": "Mediaid"
     }
   }
   ```

3. **JWT Secret Key**: Changez la clé secrète dans `appsettings.json` pour la production:
   ```json
   {
     "JwtSettings": {
       "SecretKey": "VotreCléSecrèteTrèsLongueEtComplexeIci"
     }
   }
   ```

## 🏃 Démarrage

```bash
cd server
dotnet restore
dotnet build
dotnet run
```

L'application sera accessible sur `https://localhost:5001` ou `http://localhost:5000`

## 👥 Rôles

- **Patient**: Créer et gérer des demandes d'aide
- **Aidant**: Proposer son aide aux demandes
- **Expert**: Valider les demandes sensibles
- **Admin**: Gérer la plateforme et les utilisateurs

## 🔐 Sécurité

- Authentification par cookies avec support JWT
- Hashage des mots de passe avec BCrypt (work factor 12)
- Protection contre brute force (lockout après 5 tentatives)
- Audit logs complets
- Séparation stricte des rôles (RBAC)

## 📝 Fonctionnalités Principales

- ✅ Inscription/Connexion sécurisée
- ✅ Gestion des demandes (CRUD)
- ✅ Système de propositions
- ✅ Chat en temps réel (SignalR)
- ✅ Notifications
- ✅ Système de réputation
- ✅ Validation par experts
- ✅ Tableau de bord administrateur
- ✅ Logs d'audit

## 🛠️ Développement

Pour créer un utilisateur administrateur, utilisez l'interface d'administration ou modifiez directement la base de données MongoDB.

## 📄 Licence

Ce projet est développé dans le cadre d'un projet académique.

## 🤝 Contribution

Ce projet est un travail académique. Pour toute question ou suggestion, contactez l'équipe de développement.


