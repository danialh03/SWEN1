# Media Rating Platform – REST Server (C# ohne ASP.NET)

Dieses Projekt ist ein **eigenständiger REST-Server in C# (ohne ASP.NET)**, der eine **Media Rating Platform** bereitstellt. Nutzer können sich registrieren und einloggen (token-basiert), Medien verwalten und bewerten sowie **Favoriten**, **Likes**, **Profile/Statistiken**, **Leaderboard** und **Recommendations** nutzen.

**Standard-URL:** `http://localhost:8080`

---

## Features (Überblick)

- **User**
  - Registrierung & Login (token-basiert)
  - Profil anzeigen/ändern inkl. Stats
  - Rating-History
  - Recommendations (nur für den eigenen User)
- **Media**
  - CRUD (Update/Delete nur durch Creator)
  - Search/Filter/Sort (inkl. `minScore` über Durchschnittsbewertung)
- **Ratings**
  - Create/Read/Update/Delete (Update/Delete nur Owner)
  - Kommentar-Moderation: Kommentar ist öffentlich erst nach **Confirm**
  - Likes: Like/Unlike (max. **1 Like pro User pro Rating**)
- **Favorites**
  - Favorit hinzufügen/entfernen
  - Liste eigener Favoriten
- **Leaderboard**
  - Aktivste User nach Anzahl Ratings

---

## Voraussetzungen

- **.NET SDK** (Version passend zum Projekt)
- **PostgreSQL** (lokal laufend)
- Datenbank: **`dhp`** (oder entsprechend anpassen)
- `psql` oder **pgAdmin** zum Ausführen des Schemas

---

## Konfiguration

Die Connection ist aktuell in `Database/Connection.cs` hinterlegt:

Host=localhost
Port=5432
Username=postgres
Password=postgres
Database=dhp

Wenn du andere Credentials/DB-Namen nutzt, passe diese Werte entsprechend an.

---

## Datenbank-Setup

### Option A (empfohlen): `schema.sql` ausführen

1. Datenbank erstellen (falls noch nicht vorhanden):

```sql
CREATE DATABASE dhp;
```

2. Schema importieren:

```bash
psql -h localhost -p 5432 -U postgres -d dhp -f schema.sql
```

### Option B: pgAdmin

- `schema.sql` im **Query Tool** öffnen und ausführen.

---

## Server starten

Im Ordner des Server-Projekts:

```bash
dotnet run
```

Der Server startet auf **Port 8080**.

### Hinweis (Windows): URLACL / Admin-Rechte

`HttpListener` benötigt auf Windows häufig eine URL-Reservierung. Wenn du beim Start Probleme bekommst (z. B. _Access denied_), hast du zwei Möglichkeiten:

**Variante 1:** Visual Studio / Terminal als Administrator starten (einfachster Weg)

**Variante 2:** URLACL einmalig setzen (als Admin in CMD):

```bat
netsh http add urlacl url=http://+:8080/ user=Everyone
```

Danach kann der Server in der Regel auch ohne Admin-Rechte gestartet werden.

---

## Authentifizierung (Token)

- `POST /api/users/register` und `POST /api/users/login` sind **öffentlich**.
- Alle anderen Endpoints verlangen einen **Bearer Token**.

Unterstützte Header:

- `Authentication: Bearer <token>` (laut Spec)
- `Authorization: Bearer <token>` (Standard)

Tokens werden in der DB in der Tabelle **`sessions`** gespeichert und laufen nach einer festen Zeit ab.

---

## Schnelltest mit Postman

Im Projekt liegt die Collection:

- `MRP_Final.postman_collection.json`

Empfohlener Ablauf:

1. **Register**
2. **Login** → Token kopieren
3. In Postman Header setzen: `Authentication: Bearer <token>`
4. Media erstellen / listen / filtern
5. Rating erstellen / liken / confirm
6. Favorite setzen / entfernen / Favoritenliste abrufen
7. Profil & Stats / Rating-History
8. Leaderboard
9. Recommendations

---

## Tests ausführen

Im Repo-/Solution-Root:

```bash
dotnet test
```

Das Testprojekt **`DhProjekt.Tests`** enthält Unit Tests für Helper/Auth/Scoring.

---

## Projektstruktur (kurz)

- `Server/` → `HttpListener` Server + Request/Response Handling
- `Handlers/` → Endpoints/Controller (Routing + Auth + Validierung)
- `Database/` → Repositories + SQL Zugriff + Model-Klassen
- `Auth/` → Passwort-Hashing (PBKDF2) + Tokenverwaltung (Sessions)
- `Helpers/` → Query/Header/Sort/Scoring Hilfsklassen
- `AppServices.cs` → zentrale Instanz für Repositories/DB
- `Program.cs` → Startpunkt

---

## Troubleshooting

- **DB-Verbindung schlägt fehl:** Prüfe, ob PostgreSQL läuft und die Connection-Strings in `Database/Connection.cs` korrekt sind.
- **Access denied unter Windows:** Siehe Abschnitt _Windows URLACL / Admin-Rechte_.
- **401 Unauthorized:** Stelle sicher, dass der Bearer Token im Header korrekt gesetzt ist (z. B. `Authorization` oder `Authentication`).
