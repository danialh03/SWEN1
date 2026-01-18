using System;
using System.Net;
using System.Text.Json.Nodes;
using DhProjekt.Auth;
using DhProjekt.Server;

namespace DhProjekt.Handlers
{
    public sealed class UserHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs req)
        {
            var path = req.Path;
            var method = req.Method;

            if (!path.StartsWith("/api/users"))
                return;

            // 1) Register
            if (path == "/api/users/register" && method == HttpMethod.Post)
            {
                HandleRegister(req);
                return;
            }

            // 2) Login
            if (path == "/api/users/login" && method == HttpMethod.Post)
            {
                HandleLogin(req);
                return;
            }

            // 3) Recommendations: GET /api/users/{username}/recommendations
            if (method == HttpMethod.Get && path.StartsWith("/api/users/") && path.EndsWith("/recommendations"))
            {
                HandleRecommendations(req);
                return;
            }
        }

        private void HandleRegister(HttpRestEventArgs req)
        {
            var body = req.Content;

            var username = GetString(body, "Username");
            var password = GetString(body, "Password");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                SendError(req, HttpStatusCode.BadRequest, "Username and Password are required.");
                return;
            }

            var hash = PasswordHelper.HashPassword(password);
            var user = AppServices.UserRepo.CreateUser(username, hash);

            if (user == null)
            {
                SendError(req, HttpStatusCode.Conflict, "Username already exists.");
                return;
            }

            req.Respond((int)HttpStatusCode.Created, new JsonObject
            {
                ["id"] = user.Id,
                ["username"] = user.Username
            });
        }

        private void HandleLogin(HttpRestEventArgs req)
        {
            var body = req.Content;

            var username = GetString(body, "Username");
            var password = GetString(body, "Password");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                SendError(req, HttpStatusCode.BadRequest, "Username and Password are required.");
                return;
            }

            var user = AppServices.UserRepo.GetByUsername(username);
            if (user == null)
            {
                SendError(req, HttpStatusCode.Unauthorized, "Invalid username or password.");
                return;
            }

            if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                SendError(req, HttpStatusCode.Unauthorized, "Invalid username or password.");
                return;
            }


            var token = AuthManager.CreateToken(user.Id, AppServices.SessionRepo);

            req.Respond((int)HttpStatusCode.OK, new JsonObject
            {
                ["token"] = token,
                ["username"] = user.Username,
                ["expiresInSeconds"] = (int)AuthManager.TokenLifetime.TotalSeconds
            });
        }

        private void HandleRecommendations(HttpRestEventArgs req)
        {
            // Token prüfen
            var token = GetTokenFromHeader(req.Context.Request);
            if (string.IsNullOrWhiteSpace(token))
            {
                SendError(req, HttpStatusCode.Unauthorized, "Missing or invalid Authentication/Authorization header.");
                return;
            }

            var requesterUserId = AuthManager.GetUserIdForToken(token, AppServices.SessionRepo);
            if (requesterUserId == null)
            {
                SendError(req, HttpStatusCode.Unauthorized, "Invalid or expired token.");
                return;
            }

            // Username aus URL extrahieren
            // /api/users/{username}/recommendations
            var middle = req.Path.Substring("/api/users/".Length);
            var username = middle.Replace("/recommendations", "");

            if (string.IsNullOrWhiteSpace(username))
            {
                SendError(req, HttpStatusCode.BadRequest, "Username missing in URL.");
                return;
            }

            var user = AppServices.UserRepo.GetByUsername(username);
            if (user == null)
            {
                SendError(req, HttpStatusCode.NotFound, "User not found.");
                return;
            }

            // Sicherheit: User darf nur eigene Recommendations abrufen
            if (user.Id != requesterUserId.Value)
            {
                SendError(req, HttpStatusCode.Forbidden, "You can only request your own recommendations.");
                return;
            }

            int limit = 10;
            var limitText = req.Context.Request.QueryString["limit"];
            if (!string.IsNullOrWhiteSpace(limitText) && int.TryParse(limitText, out var parsed))
                limit = parsed;

            var recs = AppServices.RecommendationRepo.GetRecommendations(user.Id, limit);

            var arr = new JsonArray();
            foreach (var (media, score, reason) in recs)
            {
                arr.Add(new JsonObject
                {
                    ["id"] = media.Id,
                    ["title"] = media.Title,
                    ["description"] = media.Description,
                    ["mediaType"] = media.MediaType,
                    ["releaseYear"] = media.ReleaseYear,
                    ["genre"] = media.Genre,
                    ["ageRestriction"] = media.AgeRestriction,
                    ["createdBy"] = media.CreatedBy,
                    ["score"] = score,
                    ["reason"] = reason
                });
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject
            {
                ["items"] = arr
            });
        }
    }
}
