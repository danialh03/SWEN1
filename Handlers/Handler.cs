using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using DhProjekt.Server;
using DhProjekt.Helpers;


namespace DhProjekt.Handlers
{
    public abstract class Handler : IHandler
    {
        private static List<IHandler>? _handlers;

        private static List<IHandler> GetHandlers()
        {
            var result = new List<IHandler>();

            foreach (var type in Assembly.GetExecutingAssembly()
                                         .GetTypes()
                                         .Where(t => typeof(IHandler).IsAssignableFrom(t) && !t.IsAbstract))
            {
                if (Activator.CreateInstance(type) is IHandler handler)
                    result.Add(handler);
            }

            return result;
        }

        public static void HandleEvent(object? sender, HttpRestEventArgs e)
        {
            foreach (var handler in _handlers ??= GetHandlers())
            {
                handler.Handle(e);
                if (e.Responded) break;
            }
        }

        public abstract void Handle(HttpRestEventArgs e);

        // Accept BOTH headers: spec uses "Authentication", standard is "Authorization"
        protected static string? GetTokenFromHeader(HttpListenerRequest httpRequest)
        {
            var authentication = httpRequest.Headers["Authentication"];
            var authorization = httpRequest.Headers["Authorization"];

            return AuthHeaderHelper.TryGetBearerToken(authorization, authentication);
        }


        // Mini Query Parser: "?a=1&b=hello" -> Dictionary
        protected static Dictionary<string, string> GetQueryParams(HttpListenerRequest req)
        {
            return QueryStringHelper.Parse(req.Url?.Query);
        }


        protected static void SendError(HttpRestEventArgs req, HttpStatusCode statusCode, string message)
        {
            var json = new JsonObject
            {
                ["success"] = false,
                ["error"] = message
            };

            req.Respond((int)statusCode, json);
        }

        protected static string? GetString(JsonObject body, string name)
        {
            if (body[name] is JsonNode node1)
                return node1.ToString();

            var camel = char.ToLower(name[0]) + name[1..];
            if (body[camel] is JsonNode node2)
                return node2.ToString();

            return null;
        }

        protected static int? GetInt(JsonObject body, string name)
        {
            var text = GetString(body, name);
            return int.TryParse(text, out var value) ? value : null;
        }
    }
}
