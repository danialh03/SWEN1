using System.Net;
using System.Text.Json.Nodes;
using DhProjekt.Auth;
using DhProjekt.Server;

namespace DhProjekt.Handlers
{
    // GET/PUT /api/users/{username}/profile
    // GET /api/users/{username}/ratings
    public sealed class UserProfileHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs req)
        {
            var path = req.Path;
            var method = req.Method;

            if (!path.StartsWith("/api/users/"))
                return;

            // Login nötig 
            var token = GetTokenFromHeader(req.Context.Request);
            if (string.IsNullOrWhiteSpace(token))
            {
                SendError(req, HttpStatusCode.Unauthorized, "Missing or invalid Authentication/Authorization header.");
                return;
            }

            var requesterId = AuthManager.GetUserIdForToken(token, AppServices.SessionRepo);
            if (requesterId == null)
            {
                SendError(req, HttpStatusCode.Unauthorized, "Invalid or expired token.");
                return;
            }

            // /api/users/{username}/profile
            if (path.EndsWith("/profile"))
            {
                var username = path.Substring("/api/users/".Length).Replace("/profile", "").Trim();
                if (string.IsNullOrWhiteSpace(username)) return;

                if (method == HttpMethod.Get)
                {
                    GetProfile(req, requesterId.Value, username);
                    return;
                }

                if (method == HttpMethod.Put)
                {
                    UpdateProfile(req, requesterId.Value, username);
                    return;
                }
            }

            // /api/users/{username}/ratings  (Rating-History)
            if (path.EndsWith("/ratings") && method == HttpMethod.Get)
            {
                var username = path.Substring("/api/users/".Length).Replace("/ratings", "").Trim();
                if (string.IsNullOrWhiteSpace(username)) return;

                GetRatingHistory(req, requesterId.Value, username);
                return;
            }
        }

        private void GetProfile(HttpRestEventArgs req, int requesterId, string username)
        {
            var user = AppServices.UserRepo.GetProfileByUsername(username);
            if (user == null)
            {
                SendError(req, HttpStatusCode.NotFound, "User not found.");
                return;
            }

            // Nur eigenes Profil
            if (user.Id != requesterId)
            {
                SendError(req, HttpStatusCode.Forbidden, "You can only view your own profile.");
                return;
            }

            var (totalRatings, avgStars, favGenre, favCount) = AppServices.UserRepo.GetUserStats(user.Id);

            var json = new JsonObject
            {
                ["id"] = user.Id,
                ["username"] = user.Username,
                ["displayName"] = user.DisplayName,
                ["bio"] = user.Bio,
                ["updatedAt"] = user.UpdatedAt,
                ["stats"] = new JsonObject
                {
                    ["totalRatings"] = totalRatings,
                    ["averageStars"] = avgStars,          // kann null sein
                    ["favoriteGenre"] = favGenre,         // kann null sein
                    ["favoritesCount"] = favCount
                }
            };

            req.Respond((int)HttpStatusCode.OK, json);
        }

        private void UpdateProfile(HttpRestEventArgs req, int requesterId, string username)
        {
            var user = AppServices.UserRepo.GetProfileByUsername(username);
            if (user == null)
            {
                SendError(req, HttpStatusCode.NotFound, "User not found.");
                return;
            }

            if (user.Id != requesterId)
            {
                SendError(req, HttpStatusCode.Forbidden, "You can only edit your own profile.");
                return;
            }

            var body = req.Content;
            var displayName = GetString(body, "DisplayName");
            var bio = GetString(body, "Bio");

            var ok = AppServices.UserRepo.UpdateProfile(user.Id, displayName, bio);
            if (!ok)
            {
                SendError(req, HttpStatusCode.InternalServerError, "Profile update failed.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }

        private void GetRatingHistory(HttpRestEventArgs req, int requesterId, string username)
        {
            var user = AppServices.UserRepo.GetByUsername(username);
            if (user == null)
            {
                SendError(req, HttpStatusCode.NotFound, "User not found.");
                return;
            }

            if (user.Id != requesterId)
            {
                SendError(req, HttpStatusCode.Forbidden, "You can only view your own rating history.");
                return;
            }

            var history = AppServices.RatingRepo.GetHistoryByUser(user.Id);

            var arr = new JsonArray();
            foreach (var (r, title) in history)
            {
                // Eigene History: Kommentar darf man immer sehen
                arr.Add(new JsonObject
                {
                    ["ratingId"] = r.Id,
                    ["mediaId"] = r.MediaId,
                    ["mediaTitle"] = title,
                    ["stars"] = r.Stars,
                    ["comment"] = r.Comment,
                    ["commentConfirmed"] = r.CommentConfirmed,
                    ["createdAt"] = r.CreatedAt,
                    ["updatedAt"] = r.UpdatedAt
                });
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["items"] = arr });
        }
    }
}
