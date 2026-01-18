# Protokoll – Media Rating Platform (REST-Backend ohne ASP.NET)

## Ziel der Anwendung

Mein Projekt ist ein REST-Backend für eine Media Rating Platform. Die Idee ist, dass Benutzer Medien wie Filme, Serien oder Games in einer Datenbank verwalten und bewerten können. Es gibt Registrierung und Login, und nach dem Login bekommt man einen Token. Mit diesem Token kann man dann die restlichen Funktionen benutzen.

Das Ziel der Abgabe ist, eine funktionierende REST-API **ohne ASP.NET** bereitzustellen, die in **PostgreSQL** speichert und die in der Spezifikation geforderten Features umsetzt (**Medien**, **Ratings**, **Likes**, **Favoriten**, **Profil/Statistiken**, **Leaderboard**, **Recommendations**).

---

## Allgemeiner Aufbau / Architektur

Ich habe das Projekt bewusst in mehrere Bereiche aufgeteilt, damit es übersichtlich bleibt:

- **Server**: nimmt HTTP-Requests an und schickt JSON-Responses zurück.
- **Handlers**: enthalten die Endpoint-Logik (Routing, Authentifizierung, Validierung und Antworten).
- **Database (Repositories + Models)**: enthält die SQL-Zugriffe auf PostgreSQL und die Datenmodelle.
- **Auth**: Passwort-Hashing und Token-Session-Verwaltung.
- **Helpers**: kleine Helfer für Header, Query-Parameter, Sortierung und Recommendation-Scoring.

### Request-Flow (Dispatch)

Der Server basiert auf `HttpListener`. Für jede eingehende Anfrage wird ein `HttpRestEventArgs` erzeugt (**Method**, **Path**, **Body** als JSON). Danach wird der Request über ein zentrales Dispatch-System an alle Handler-Klassen weitergegeben. Jeder Handler prüft, ob er für den Pfad zuständig ist. Sobald ein Handler eine Antwort sendet, wird abgebrochen.

---

## Wichtige Klassen und wofür sie da sind

### Server

- **HttpRestServer**: kapselt den `HttpListener`, hört auf Port **8080** und löst pro Request ein Event aus.
- **HttpRestEventArgs**: enthält die Request-Daten (Methode, Pfad, JSON-Body) und eine `Respond(...)`-Methode, um eine Antwort zu senden.

### Handlers

- **IHandler**: Interface für alle Request-Handler.
- **Handler**: Basisklasse mit der zentralen `HandleEvent(...)`-Methode (Dispatch) und Hilfsfunktionen (Token aus Header, Fehlerantworten, JSON-Parsing).
- **UserHandler**: Registrierung, Login und Recommendations (nur für den eigenen User).
- **UserProfileHandler**: Profil anzeigen/ändern, Stats anzeigen und eigene Rating-History abrufen.
- **MediaHandler**: Medien erstellen, listen (mit Search/Filter/Sort), einzelne Medien lesen, updaten und löschen (Update/Delete nur durch Ersteller).
- **RatingHandler**: Ratings erstellen/lesen, bearbeiten/löschen (nur Owner), Kommentar bestätigen (Confirm), Likes setzen und entfernen.
- **FavoriteHandler**: Favoriten setzen/entfernen und eigene Favoritenliste ausgeben.
- **LeaderboardHandler**: liefert die aktivsten User basierend auf Anzahl der Ratings.

### Database

- **DatabaseConnection**: öffnet eine Verbindung zur PostgreSQL Datenbank.

#### Repositories

- **UserRepository**: User erstellen, User lesen, Profil/Stats aktualisieren/ausgeben.
- **SessionRepository**: Tokens (Sessions) speichern, prüfen, Ablauf kontrollieren (Expired Tokens werden ungültig).
- **MediaRepository**: Media CRUD und Filter/Sort Logik.
- **RatingRepository**: Ratings + Like/Unlike, Stats pro Media (Avg + Count), Leaderboard und Rating-History.
- **FavoriteRepository**: Favoriten add/remove/list.
- **RecommendationRepository**: baut Recommendations basierend auf bisherigen hohen Bewertungen.

#### Models

- **User**, **MediaItem**, **Rating**: einfache Datenklassen zu den Tabellen.

### Auth

- **PasswordHelper**: PBKDF2-Hashing mit Salt für neue Passwörter und Verify-Funktion (inkl. Legacy-SHA256 Unterstützung für alte Hashes).
- **AuthManager**: erzeugt Tokens und speichert sie über `SessionRepository` in der DB, inklusive Ablaufzeit.

### Helpers

- **AuthHeaderHelper**: liest Bearer Token aus `Authentication` oder `Authorization` Header.
- **QueryStringHelper**: parst Query-Parameter.
- **MediaSortHelper**: sorgt für sichere Sortierung (nur erlaubte Felder).
- **RecommendationScoring**: berechnet Score/Reason für Empfehlungen (wird auch in Tests verwendet).

---

## Datenbank (Tabellen und Regeln)

### Tabellen

- **users**: Benutzer, Passwort-Hash, Profilinfos
- **sessions**: Token + Ablaufzeit (damit Login/Logout/Expire möglich ist)
- **media**: Medien (Filme/Serien/Games) inklusive `created_by`
- **ratings**: Bewertungen (1–5 Sterne) + optional Kommentar + `comment_confirmed`
- **rating_likes**: Likes auf Ratings
- **favorites**: Favoriten eines Users

### Wichtige technische Regeln (Constraints)

- **Username ist UNIQUE**
- Pro User und Medium darf nur ein Rating existieren (**UNIQUE `user_id` + `media_id`**)
- Pro User und Rating darf es nur einen Like geben (**PRIMARY KEY `user_id` + `rating_id`**)
- Pro User und Medium darf es nur einen Favorite geben (**PRIMARY KEY `user_id` + `media_id`**)
- Sterne müssen zwischen **1 und 5** sein (**CHECK Constraint**)

---

## Welche Funktionen sind final umgesetzt?

### User

- Registrierung (User anlegen)
- Login (Token erstellen und in `sessions` speichern)
- Profil lesen und ändern (nur eigenes Profil)
- Stats im Profil (z. B. `totalRatings`, `averageStars`, `favoriteGenre`, `favoritesCount`)
- Rating-History (nur eigene History)
- Recommendations (nur für den eigenen User, optional `limit`)

### Media

- Liste (mit Search/Filter/Sort, z. B. `search`, `genre`, `mediaType`, `releaseYear`, `maxAgeRestriction`, `minScore`, `sort`, `order`)
- einzelnes Medium anzeigen
- Medium erstellen (`created_by` = eingeloggter User)
- Medium bearbeiten/löschen (nur Ersteller)

### Ratings

- Rating erstellen pro Medium (nur 1 Rating pro User pro Medium)
- Ratings zu einem Medium ausgeben (inkl. LikeCount)
- Rating bearbeiten/löschen (nur Owner)
- Comment Confirm (Kommentar wird öffentlich erst nach Confirm; Owner sieht eigenen Kommentar immer)
- Like / Unlike (pro User nur einmal möglich)

### Favorites

- Favorite setzen/entfernen
- Eigene Favoritenliste abrufen

### Leaderboard

- User nach Anzahl Ratings sortiert, optional `limit`

---

## Tests

Es gibt ein eigenes Testprojekt (**DhProjekt.Tests**). Die Tests decken wichtige Helper- und Auth-Funktionen sowie die Recommendation-Scoring Logik ab. Damit wird sichergestellt, dass z. B. Header-Parsing, Query-Parsing, Sort-Auswahl und Password-Verify korrekt funktionieren.

Tests ausführen:

```bash
dotnet test
```

---

## Besondere Punkte / Entscheidungen / Probleme

- **HttpListener auf Windows**: manchmal braucht man Administrator-Rechte oder URLACL (`netsh`), sonst kann Port 8080 nicht gebunden werden.
- **Passwort-Hashing**: Ursprünglich SHA256, final PBKDF2 mit Salt (sicherer). Zusätzlich gibt es eine Legacy-Verify, damit bestehende DB-User weiterhin loginfähig sind.
- **Handler-Matching**: Handler prüfen möglichst exakt, ob sie für einen Pfad zuständig sind, damit es keine Nebenwirkungen beim Dispatch gibt (z. B. Favorites-Handler nur auf Favorites-Routen).
- **Recommendations**: Umsetzung ist regelbasiert und basiert auf hoch bewerteten Medien (>= 4) sowie Genre/MediaType/AgeRestriction.
