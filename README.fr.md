# KodiNet

Application web de gestion centralisée de Raspberry Pi sous LibreELEC/Kodi.

Permet de surveiller l'état des lecteurs, contrôler la lecture, transférer des fichiers vidéo depuis un serveur de stockage privé vers les Pi, et planifier des tâches automatiques.

L'application a d'abord été pensée pour l'utilisation de vidéos (dossier */storage/videos* sur le Raspberry Pi).
Par conséquent, l'utilisation d'autres formats de fichiers n'a pas été testée.

For English, see [README.md](README.md)

---

## Table des matières

1. [Prérequis](#prérequis)
2. [Architecture](#architecture)
3. [Installation rapide (Docker)](#installation-rapide-docker)
4. [Configuration](#configuration)
5. [Activation JSON-RPC sur Kodi](#activation-json-rpc-sur-kodi)
6. [Serveur de stockage privé](#serveur-de-stockage-privé)
7. [Authentification Microsoft](#authentification-microsoft)
8. [Rôles applicatifs](#rôles-applicatifs)
9. [Fonctionnalités](#fonctionnalités)
10. [Développement local](#développement-local)
11. [Migrations de base de données](#migrations-de-base-de-données)
12. [Sécurité](#sécurité)
13. [Licence](#licence)

---

## Prérequis

| Composant | Version minimale |
|---|---|
| Raspberry Pi | 4B et plus |
| LibreELEC | 10.0 |
| Kodi | 19 (Matrix) |
| MySQL | 8.0 |
| Docker + Docker Compose | 24.0 |
| Compte Microsoft | Azure AD / Entra ID organisationnel |

Les prérequis reflètent seulement l'environnement pour lequel l'application a été développée ; il est peut être tout à fait possible d'utiliser une version antérieure du Raspberry Pi, etc.

---

## Architecture

```
┌─────────────────────────────────────────┐
│  Internet                               │
│          │  HTTPS (OIDC Microsoft)      │
│    ┌─────▼──────────┐                   │
│    │  KodiNet Web   │ Blazor Server     │
│    │  (conteneur)   │ .NET 8            │
│    └──┬──────────┬──┘                   │
│  Réseau│interne   │                     │
│    ┌───▼───┐  ┌───▼──────────┐          │
│    │ MySQL │  │   Stockage   │          │
│    │  8.4  │  │   Privé      │          │
│    └───────┘  │  (FastAPI)   │          │
│               └──────────────┘          │
│                                         │
│  Réseau local Pi                        │
│    ┌────────┐ ┌────────┐ ┌────────┐     │
│    │  Pi 1  │ │  Pi 2  │ │  Pi N  │     │
│    │  Kodi  │ │  Kodi  │ │  Kodi  │     │
│    └────────┘ └────────┘ └────────┘     │
└─────────────────────────────────────────┘
```

**Couches applicatives :**

```
KodiNet.Domain          Entités, énumérations, interfaces pures
KodiNet.Application     DTOs, interfaces services, logique métier
KodiNet.Infrastructure  EF Core/MySQL, clients Kodi/SFTP/HTTP, chiffrement
KodiNet.Web             Blazor Server, composants MudBlazor, pages
```

---

## Installation rapide (Docker)

### 1. Cloner le dépôt

```bash
git clone https://github.com/votre-compte/kodinet.git
cd kodinet
```

### 2. Créer le fichier d'environnement

```bash
cp .env.example .env
```

Éditer `.env` avec vos valeurs :

```env
# Azure AD
AZUREAD_TENANTID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
AZUREAD_CLIENTID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
AZUREAD_CLIENTSECRET=votre_secret_azure

# Base de données
MYSQL_ROOT_PASSWORD=mot_de_passe_root_fort
MYSQL_DATABASE=kodinet
MYSQL_USER=kodinet_user
MYSQL_PASSWORD=mot_de_passe_db_fort

# Serveur de stockage privé
STORAGE_API_KEY=cle_api_stockage_longue_et_aleatoire
STORAGE_MAX_SIZE_MB=4096
```

### 3. Démarrer le stack

```bash
docker compose up -d --build
```

L'application est disponible sur `http://localhost:8080`.

Les migrations de base de données sont appliquées automatiquement au démarrage.

### 4. Premier accès

Se connecter avec un compte Microsoft de votre organisation. Le premier utilisateur qui se connecte reçoit automatiquement le rôle **Owner**.

---

## Configuration

### `appsettings.json` (structure — les valeurs sont dans `.env`)

```json
{
  "AzureAd": {
    "Instance":     "https://login.microsoftonline.com/",
    "TenantId":     "",
    "ClientId":     "",
    "ClientSecret": "",
    "CallbackPath": "/signin-oidc"
  },
  "Storage": {
    "BaseUrl":  "http://storage:8090",
    "ApiKey":   "",
    "RootPath": "/data"
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  }
}
```

Toutes les valeurs sensibles sont injectées via variables d'environnement Docker (format `Section__Clé`).

### Configuration SMTP (depuis l'interface)

La configuration mail est gérée depuis **Paramètres → E-mail SMTP**, accessible uniquement au rôle Owner. Le mot de passe SMTP est chiffré en base via AES (DataProtection API).

---

## Activation JSON-RPC sur Kodi

Sur chaque Raspberry Pi, depuis l'interface Kodi :

1. **Paramètres** → **Services** → **Contrôle**
2. Activer **Autoriser le contrôle à distance HTTP**
3. Optionnel : configurer un nom d'utilisateur et un mot de passe
4. Le port par défaut est `8080`

---

## Serveur de stockage privé

Le serveur de stockage est inclus dans le stack Docker. Il expose une API REST sur le port `8090` (accessible uniquement depuis le conteneur web, pas depuis l'extérieur).

### Endpoints exposés

| Méthode | URL | Description |
|---|---|---|
| `GET` | `/api/files?path=/dossier` | Lister le contenu d'un dossier |
| `GET` | `/api/files/download?path=/fichier` | Télécharger un fichier |
| `POST` | `/api/files/upload?path=/dossier` | Uploader un fichier (multipart) |
| `POST` | `/api/files/folder?path=/dossier` | Créer un dossier |
| `DELETE` | `/api/files?path=/element` | Supprimer un fichier ou dossier |
| `PATCH` | `/api/files/rename` | Renommer |
| `PATCH` | `/api/files/move` | Déplacer |

**Authentification** : header `X-Api-Key` avec la valeur de `STORAGE_API_KEY`.

### Limite de taille

Configurable via `STORAGE_MAX_SIZE_MB` dans `.env`. Par défaut : 4096 Mo (4 Go).

### Volume de données

Les fichiers sont stockés dans le volume Docker `storage_data` (persistant). Pour monter un répertoire existant de l'hôte :

```yaml
# Dans docker-compose.yml
volumes:
  - /chemin/sur/hôte/videos:/data
```

---

## Authentification Microsoft

### Créer une App Registration dans Azure Entra ID

1. Portail Azure → **Azure Active Directory** → **App registrations** → **New registration**
2. **Name** : KodiNet
3. **Supported account types** : Accounts in this organizational directory only
4. **Redirect URI** : `https://votre-domaine.com/signin-oidc` (Web)
5. Après création, noter **Application (client) ID** → `AZUREAD_CLIENTID`
6. Répertoire → **Directory (tenant) ID** → `AZUREAD_TENANTID`
7. **Certificates & secrets** → New client secret → noter la valeur → `AZUREAD_CLIENTSECRET`

### Permissions requises

Aucune permission Graph nécessaire — KodiNet utilise uniquement l'authentification OIDC de base (OpenID + profile + email).

---

## Rôles applicatifs

Les rôles sont gérés depuis **Paramètres → Utilisateurs**. Ils sont stockés dans la base de données interne, indépendamment d'Azure AD.

| Rôle | Attribut | Droits |
|---|---|---|
| **Owner** | Unique — non révocable — non attribuable depuis l'UI | Tous les droits Admin + configuration SMTP + gestion des thèmes + import/export + transfert de propriété |
| **Admin** | Géré par Owner | Gestion des Pi, établissements, utilisateurs, planificateur, notifications |
| **Operator** | Géré par Admin | Contrôle des lecteurs Kodi, transferts de fichiers |
| **Viewer** | Géré par Admin | Lecture seule — consultation de l'état des Pi |

### Premier Owner

Le premier utilisateur qui se connecte à l'application reçoit automatiquement le rôle Owner. Ce mécanisme ne se déclenche qu'une seule fois (quand la table `user_roles` est vide).

### Transfert de propriété Owner

Depuis **Paramètres → Utilisateurs**, le bouton "Transférer le rôle Owner" permet à l'Owner de déléguer son rôle à un autre utilisateur. L'Owner actuel est immédiatement déconnecté après le transfert.

---

## Fonctionnalités

### Tableau de bord

- Grille des Pi avec statut en temps réel (Lecture / Veille / Hors ligne / Incompatible)
- Filtre par établissement, état, recherche texte
- Tri par nom, état, établissement (croissant/décroissant)
- Import de Pi en lot depuis un fichier CSV
- Stats globales dans la barre d'en-tête (total / en lecture / hors ligne)

**Format CSV d'import Pi :**
```
name,ip_address,location,model,kodi_user,kodi_password,kodi_port,ssh_user,ssh_password,ssh_port,video_folder
Pi Salle A,192.168.1.10,Bâtiment Nord,Pi 4B,kodi,motdepasse,8080,pi,motdepasse,22,/storage/videos
```

### Panneau de détail d'un Pi

**Onglet Infos & État**
- Statut Kodi, version, température CPU, mémoire libre, espace disque
- Contrôle du volume
- Informations de configuration

**Onglet Lecteur & Fichiers**
- Lecture/pause, piste précédente/suivante, arrêt
- Mode répétition (Off / Une piste / Tout)
- Navigation dans les fichiers via SFTP
- Envoi de fichiers depuis le navigateur vers le Pi
- Renommage de fichiers
- Accès direct à l'interface web Kodi (lien)

### Stockage privé

- Navigation dans l'arborescence des fichiers du serveur de stockage
- Import de fichiers vidéo depuis le navigateur
- Création de dossiers, renommage, déplacement, suppression
- Envoi de fichiers sélectionnés vers un ou plusieurs Pi (transfert en arrière-plan)
- File de transfert avec suivi de progression
- Journal des opérations storage (import, suppression, renommage, déplacement)

### Paramètres

**Utilisateurs (Admin)** — Gestion des rôles et activation des notifications cron par utilisateur

**Établissements (Admin)** — CRUD des établissements + import/export CSV

**Planificateur (Admin)**
- Job `KodiRestarter` : relance les lecteurs inactifs (Idle/Stopped) et joue le dossier vidéo en répétition
- Job `KodiRebooter` : redémarre le système LibreELEC de tous les Pi
- Configuration de l'expression cron par job
- Activation/désactivation par job
- Historique d'exécution avec détail par Pi

**Thèmes (Owner)** — Création, modification, suppression de thèmes personnalisés

**E-mail SMTP (Owner)** — Configuration du serveur d'envoi (chiffrement AES du mot de passe)

### Thèmes

4 thèmes inclus : **Ruby** (bordeaux), **Ocean** (pétrole), **Amber** (bois chaud). Chaque thème dispose d'un mode clair et sombre. Le mode (Système / Clair / Sombre) est mémorisé par appareil dans `localStorage`.

### Multilingue

L'interface est disponible en **français** et **anglais**. La langue se sélectionne depuis le menu utilisateur (icône avatar en haut à droite). Le choix est persisté par compte utilisateur et synchronisé entre tous les appareils connectés.

---

## Développement local

### Prérequis

- .NET 8 SDK
- MySQL 8.0 (local ou Docker)
- Python 3.12+ (pour le serveur de stockage)

### Démarrage

```bash
# Cloner
git clone https://github.com/mathiasbenner/kodinet.git
cd kodinet

# Restaurer les packages
dotnet restore KodiNet.sln

# Configurer les secrets locaux (ne jamais committer)
cd KodiNet.Web
dotnet user-secrets set "AzureAd:TenantId"     "votre-tenant-id"
dotnet user-secrets set "AzureAd:ClientId"     "votre-client-id"
dotnet user-secrets set "AzureAd:ClientSecret" "votre-secret"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;..."
dotnet user-secrets set "Storage:ApiKey" "votre-cle"

# Démarrer le serveur de stockage (optionnel)
cd ../storage-server
pip install -r requirements.txt
python main.py

# Démarrer l'application
cd ../KodiNet.Web
dotnet run
```

### `docker-compose.override.yml` (dev uniquement, gitignored)

```yaml
services:
  db:
    ports:
      - "3306:3306"   # accès direct depuis l'IDE
  web:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
```

---

## Migrations de base de données

Les migrations sont appliquées automatiquement au démarrage de l'application.

Pour créer une nouvelle migration après modification d'entité :

```bash
dotnet ef migrations add NomDeLaMigration \
  --project KodiNet.Infrastructure \
  --startup-project KodiNet.Web
```

---

## Sécurité

- **Authentification** : OIDC Microsoft — seuls les comptes de votre organisation Azure AD peuvent se connecter
- **Autorisation** : rôles applicatifs stockés en base, chargés via `IClaimsTransformation`
- **Credentials Kodi/SSH** : chiffrés en base via AES (DataProtection API)
- **Mot de passe SMTP** : chiffré en base via AES
- **API Key stockage** : injectée via variable d'environnement, jamais exposée côté client
- **Réseau Docker** : MySQL et serveur de stockage sur réseau `internal: true` — inaccessibles depuis l'extérieur
- **Aucun secret dans le dépôt** : `.env` et `appsettings.*.json` sont gitignorés

---

## Licence

MIT — voir [LICENSE](LICENSE).

Les dépendances principales et leurs licences :

| Package | Licence |
|---|---|
| MudBlazor | MIT |
| Microsoft.Identity.Web | MIT |
| Entity Framework Core | MIT |
| Pomelo.EntityFrameworkCore.MySql | MIT |
| SSH.NET | MIT |
| Cronos | MIT |
