using System.Net;
using System.Text.Json.Nodes;
using DhProjekt.Auth;
using DhProjekt.Server;

namespace DhProjekt.Handlers
{
    /// GET /api/leaderboard?limit=10
    public sealed class LeaderboardHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs req)
        {
            var path = req.Path;
            var method = req.Method;

            if (path != "/api/leaderboard")
                return;

            if (method != HttpMethod.Get)
                return;

            // Laut Spec: alles außer register/login braucht Token
            var token = GetTokenFromHeader(req.Context.Request);
            if (string.IsNullOrWhiteSpace(token))
            {
                SendError(req, HttpStatusCode.Unauthorized, "Missing or invalid Authentication/Authorization header.");
                return;
            }

            var userId = AuthManager.GetUserIdForToken(token, AppServices.SessionRepo);
            if (userId == null)
            {
                SendError(req, HttpStatusCode.Unauthorized, "Invalid or expired token.");
                return;
            }

            // optional: ?limit=...
            int limit = 10;
            var limitText = req.Context.Request.QueryString["limit"];
            if (!string.IsNullOrWhiteSpace(limitText) && int.TryParse(limitText, out var parsed))
                limit = parsed;

            var list = AppServices.RatingRepo.GetLeaderboard(limit);

            var arr = new JsonArray();
            foreach (var (uid, username, count) in list)
            {
                arr.Add(new JsonObject
                {
                    ["userId"] = uid,
                    ["username"] = username,
                    ["ratingsCount"] = count
                });
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject
            {
                ["items"] = arr
            });
        }
    }
}
