Der Server läuft unter:
http://localhost:8080

## Voraussetzungen

- .NET SDK
- PostgreSQL (lokal laufend)
- Eine Datenbank, z.B. `dhp`
  (Connection String aktuell in `Database/Connection.cs` hinterlegt)

Die Tabellen `users` und `media` müssen in dieser Datenbank existieren.

## Anwendung starten

1. PostgreSQL starten und sicherstellen, dass die DB zum Connection String passt.

ich starte immer visual studio als Admin, weil sonst sagt Windows nicht jeder normale User darf einfach beliebige HTTP-Adressen/Ports reservieren.
