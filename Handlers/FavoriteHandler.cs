using System.Net;
using System.Text.Json.Nodes;
using DhProjekt.Auth;
using DhProjekt.Database;
using DhProjekt.Server;

namespace DhProjekt.Handlers
{
    public sealed class FavoriteHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs req)
        {
            var path = req.Path;
            var method = req.Method;

            // Match nur exakt die Favorites-Routen
            bool isList = path == "/api/users/me/favorites" && method == HttpMethod.Get;
            bool isFavoriteToggle = path.StartsWith("/api/media/") && path.EndsWith("/favorite") &&
                                    (method == HttpMethod.Post || method == HttpMethod.Delete);

            if (!isList && !isFavoriteToggle)
                return;

            // Login nötig
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

            if (isList)
            {
                ListFavorites(req, userId.Value);
                return;
            }

            // /api/media/{id}/favorite
            // sicher mediaId extrahieren
            var middle = path.Substring("/api/media/".Length); // "{id}/favorite"
            var mediaIdText = middle.Replace("/favorite", "");

            if (!int.TryParse(mediaIdText, out var mediaId))
            {
                SendError(req, HttpStatusCode.BadRequest, "Invalid media id.");
                return;
            }

            if (method == HttpMethod.Post)
            {
                AddFavorite(req, userId.Value, mediaId);
                return;
            }

            if (method == HttpMethod.Delete)
            {
                RemoveFavorite(req, userId.Value, mediaId);
                return;
            }
        }

        private void AddFavorite(HttpRestEventArgs req, int userId, int mediaId)
        {
            var media = AppServices.MediaRepo.GetById(mediaId);
            if (media == null)
            {
                SendError(req, HttpStatusCode.NotFound, "Media not found.");
                return;
            }

            var ok = AppServices.FavoriteRepo.Add(userId, mediaId);
            if (!ok)
            {
                SendError(req, HttpStatusCode.Conflict, "Media is already in favorites.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }

        private void RemoveFavorite(HttpRestEventArgs req, int userId, int mediaId)
        {
            var ok = AppServices.FavoriteRepo.Remove(userId, mediaId);
            if (!ok)
            {
                SendError(req, HttpStatusCode.NotFound, "Favorite not found.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }

        private void ListFavorites(HttpRestEventArgs req, int userId)
        {
            var favorites = AppServices.FavoriteRepo.GetFavoritesByUserId(userId);

            var arr = new JsonArray();
            foreach (var m in favorites)
            {
                arr.Add(MediaToJsonWithStats(m));
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject
            {
                ["items"] = arr
            });
        }

        private static JsonObject MediaToJsonWithStats(MediaItem m)
        {
            var (avg, count) = AppServices.RatingRepo.GetMediaStats(m.Id);

            var json = new JsonObject
            {
                ["id"] = m.Id,
                ["title"] = m.Title,
                ["description"] = m.Description,
                ["mediaType"] = m.MediaType,
                ["createdBy"] = m.CreatedBy,
                ["ratingsCount"] = count
            };

            if (avg != null)
                json["averageScore"] = avg.Value;

            if (m.ReleaseYear.HasValue) json["releaseYear"] = m.ReleaseYear.Value;
            if (m.Genre != null) json["genre"] = m.Genre;
            if (m.AgeRestriction.HasValue) json["ageRestriction"] = m.AgeRestriction.Value;

            return json;
        }
    }
}
