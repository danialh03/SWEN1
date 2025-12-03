using System;
using System.Net;
using System.Text.Json.Nodes;
using DhProjekt.Auth;
using DhProjekt.Server;
using DhProjekt.Database;

namespace DhProjekt.Handlers
{
    // Handler für User-Endpoints: Registrierung & Login
    public sealed class UserHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs req)
        {
            var path = req.Path;
            var method = req.Method;

            //  kümmert sich nur um /api/users/...
            if (!path.StartsWith("/api/users"))
                return;

            // POST /api/users/register
            if (path == "/api/users/register" && method == HttpMethod.Post)
            {
                HandleRegister(req);
                return;
            }

            // POST /api/users/login
            if (path == "/api/users/login" && method == HttpMethod.Post)
            {
                HandleLogin(req);
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
                // Username existiert schon
                SendError(req, HttpStatusCode.Conflict, "Username already exists.");
                return;
            }

            var json = new JsonObject
            {
                ["id"] = user.Id,
                ["username"] = user.Username
            };

            req.Respond((int)HttpStatusCode.Created, json);
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

            var hash = PasswordHelper.HashPassword(password);
            if (!string.Equals(hash, user.PasswordHash, StringComparison.Ordinal))
            {
                SendError(req, HttpStatusCode.Unauthorized, "Invalid username or password.");
                return;
            }

            var token = AuthManager.CreateToken(user.Id, user.Username);

            var json = new JsonObject
            {
                ["token"] = token,
                ["username"] = user.Username
            };

            req.Respond((int)HttpStatusCode.OK, json);
        }
    }
}
