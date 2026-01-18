using System;
using DhProjekt;
using DhProjekt.Handlers;
using DhProjekt.Server;


try
{
    AppServices.Db.TestConnection();
}
catch
{
    Console.WriteLine("Achtung: Datenbank ist nicht erreichbar. Server startet trotzdem.");
}

// HTTP-Server auf Port 8080
var server = new HttpRestServer(8080);

server.RequestReceived += Handler.HandleEvent;

Console.WriteLine("Server läuft auf http://localhost:8080");
server.Run();
