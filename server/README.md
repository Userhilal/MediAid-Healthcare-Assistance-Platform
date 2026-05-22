# MediAid - Plateforme Web d'Assistance MÃ©dicale Communautaire

## ðŸ“‹ Description

MediAid est une plateforme web sÃ©curisÃ©e basÃ©e sur ASP.NET Core MVC (.NET 8) permettant la mise en relation entre patients et aidants pour une assistance mÃ©dicale non urgente, sous supervision d'experts et d'administrateurs.

## ðŸ—ï¸ Architecture

- **Backend**: ASP.NET Core MVC (.NET 8)
- **Base de donnÃ©es**: MongoDB (local)
- **Authentification**: Cookie-based avec support JWT
- **Temps rÃ©el**: SignalR pour le chat
- **Frontend**: Razor Views + Bootstrap 5

## ðŸ“ Structure du Projet

```
server/
â”œâ”€â”€ Controllers/       # ContrÃ´leurs MVC
â”œâ”€â”€ Data/             # Contexte MongoDB
â”œâ”€â”€ DTOs/             # Data Transfer Objects
â”œâ”€â”€ Hubs/             # SignalR Hubs
â”œâ”€â”€ Models/           # ModÃ¨les de domaine
â”œâ”€â”€ Services/         # Services mÃ©tier
â”œâ”€â”€ Views/            # Vues Razor
â””â”€â”€ wwwroot/          # Fichiers statiques (CSS, JS)
```

## ðŸš€ PrÃ©requis

- .NET 8 SDK
- MongoDB (local ou distant)
- Visual Studio 2022 ou VS Code

## âš™ï¸ Configuration

1. **MongoDB**: Assurez-vous que MongoDB est installÃ© et fonctionne sur `mongodb://localhost:27017`

2. **Configuration**: Modifiez `appsettings.json` si nÃ©cessaire:
   ```json
   {
     "ConnectionStrings": {
       "MongoDB": "mongodb://localhost:27017",
       "DatabaseName": "Mediaid"
     }
   }
   ```

3. **JWT Secret Key**: Changez la clÃ© secrÃ¨te dans `appsettings.json` pour la production:
   ```json
   {
     "JwtSettings": {
       "SecretKey": "VotreClÃ©SecrÃ¨teTrÃ¨sLongueEtComplexeIci"
     }
   }
   ```

## ðŸƒ DÃ©marrage

```bash
cd server
dotnet restore
dotnet build
dotnet run
```

L'application sera accessible sur `https://localhost:5001` ou `http://localhost:5000`

## ðŸ‘¥ RÃ´les

- **Patient**: CrÃ©er et gÃ©rer des demandes d'aide
- **Aidant**: Proposer son aide aux demandes
- **Expert**: Valider les demandes sensibles
- **Admin**: GÃ©rer la plateforme et les utilisateurs

## ðŸ” SÃ©curitÃ©

- Authentification par cookies avec support JWT
- Hashage des mots de passe avec BCrypt (work factor 12)
- Protection contre brute force (lockout aprÃ¨s 5 tentatives)
- Audit logs complets
- SÃ©paration stricte des rÃ´les (RBAC)

## ðŸ“ FonctionnalitÃ©s Principales

- âœ… Inscription/Connexion sÃ©curisÃ©e
- âœ… Gestion des demandes (CRUD)
- âœ… SystÃ¨me de propositions
- âœ… Chat en temps rÃ©el (SignalR)
- âœ… Notifications
- âœ… SystÃ¨me de rÃ©putation
- âœ… Validation par experts
- âœ… Tableau de bord administrateur
- âœ… Logs d'audit

## ðŸ› ï¸ DÃ©veloppement

Pour crÃ©er un utilisateur administrateur, utilisez l'interface d'administration ou modifiez directement la base de donnÃ©es MongoDB.

## ðŸ“„ Licence

Ce projet est dÃ©veloppÃ© dans le cadre d'un projet acadÃ©mique.

## ðŸ¤ Contribution

Ce projet est un travail acadÃ©mique. Pour toute question ou suggestion, contactez l'Ã©quipe de dÃ©veloppement.



