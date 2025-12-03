using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using DhProjekt.Server;

namespace DhProjekt.Handlers
{
    /// Basis-Klasse für alle Handler + zentraler Event-Handler für den Server.

    public abstract class Handler : IHandler
    {
        // Cache der gefundenen Handler-Instanzen
        private static List<IHandler>? _handlers;


        /// Sucht alle Klassen im aktuellen Assembly, die IHandler implementieren
        /// und erzeugt Instanzen davon

        private static List<IHandler> GetHandlers()
        {
            var result = new List<IHandler>();

            foreach (var type in Assembly.GetExecutingAssembly()
                                         .GetTypes()
                                         .Where(t => typeof(IHandler).IsAssignableFrom(t) && !t.IsAbstract))
            {
                if (Activator.CreateInstance(type) is IHandler handler)
                {
                    result.Add(handler);
                }
            }

            return result;
        }


        /// Wird vom Server bei jedem Request aufgerufen.
        /// Verteilt den Request an alle Handler, bis einer antwortet.

        public static void HandleEvent(object? sender, HttpRestEventArgs e)
        {
            foreach (var handler in _handlers ??= GetHandlers())
            {
                handler.Handle(e);
                if (e.Responded) break; // jemand hat geantwortet -> fertig
            }
        }

        // Abstrakte Methode, die jeder konkrete Handler implementiert.
        public abstract void Handle(HttpRestEventArgs e);


        // Hilfsmethoden, die alle Handler verwenden können

        /// Liest den Token aus dem Header "Authentication: Bearer &lt;token&gt;".
        protected static string? GetTokenFromHeader(HttpListenerRequest httpRequest)
        {
            var header = httpRequest.Headers["Authentication"];
            if (string.IsNullOrWhiteSpace(header)) return null;

            const string prefix = "Bearer ";
            if (!header.StartsWith(prefix)) return null;

            return header[prefix.Length..].Trim();
        }

        /// Einheitliche Fehlerantwort
        protected static void SendError(HttpRestEventArgs req, HttpStatusCode statusCode, string message)
        {
            var json = new JsonObject
            {
                ["success"] = false,
                ["error"] = message
            };

            req.Respond((int)statusCode, json);
        }

        /// Hilfsfunktion: String aus JSON holen ("Title" oder "title").
        protected static string? GetString(JsonObject body, string name)
        {
            if (body[name] is JsonNode node1)
                return node1.ToString();

            var camel = char.ToLower(name[0]) + name[1..];
            if (body[camel] is JsonNode node2)
                return node2.ToString();

            return null;
        }

        /// Hilfsfunktion: int? aus JSON holen.
        protected static int? GetInt(JsonObject body, string name)
        {
            var text = GetString(body, name);
            return int.TryParse(text, out var value) ? value : null;
        }
    }
}
