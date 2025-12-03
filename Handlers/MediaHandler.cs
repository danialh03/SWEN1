using System.Net;
using System.Text.Json.Nodes;
using DhProjekt;
using DhProjekt.Auth;
using DhProjekt.Database;
using DhProjekt.Server;

namespace DhProjekt.Handlers
{
    /// Handler für Media-Endpoints (CRUD).
    public sealed class MediaHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs req)
        {
            var path = req.Path;
            var method = req.Method;

            // Nur /api/media... wird hier behandelt
            if (!path.StartsWith("/api/media"))
                return;

            // 1) Auth-Check: alle Media-Routen brauchen ein gültiges Token
            var token = GetTokenFromHeader(req.Context.Request);
            if (string.IsNullOrWhiteSpace(token))
            {
                SendError(req, HttpStatusCode.Unauthorized, "Missing or invalid Authentication header.");
                return;
            }

            var userId = AuthManager.GetUserIdForToken(token);
            if (userId == null)
            {
                SendError(req, HttpStatusCode.Unauthorized, "Invalid or expired token.");
                return;
            }

            // 2) /api/media  (ohne Id)
            if (path == "/api/media")
            {
                if (method == HttpMethod.Get)
                {
                    ListMedia(req);
                    return;
                }

                if (method == HttpMethod.Post)
                {
                    CreateMedia(req, userId.Value);
                    return;
                }
            }

            // 3) /api/media/{id}
            if (path.StartsWith("/api/media/"))
            {
                var idText = path.Substring("/api/media/".Length);
                if (int.TryParse(idText, out var id))
                {
                    if (method == HttpMethod.Get)
                    {
                        GetMediaById(req, id);
                        return;
                    }

                    if (method == HttpMethod.Put)
                    {
                        UpdateMedia(req, id, userId.Value);
                        return;
                    }

                    if (method == HttpMethod.Delete)
                    {
                        DeleteMedia(req, id, userId.Value);
                        return;
                    }
                }
            }

            // Wenn wir hier landen, macht dieser Handler nichts -> evtl. anderer Handler
        }

        private void CreateMedia(HttpRestEventArgs req, int userId)
        {
            var body = req.Content;

            var title = GetString(body, "Title");
            var mediaType = GetString(body, "MediaType");
            var description = GetString(body, "Description");
            var genre = GetString(body, "Genre");
            var year = GetInt(body, "ReleaseYear");
            var age = GetInt(body, "AgeRestriction");

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(mediaType))
            {
                SendError(req, HttpStatusCode.BadRequest, "Title and MediaType are required.");
                return;
            }

            var item = new MediaItem
            {
                Title = title!,
                Description = description,
                MediaType = mediaType,
                ReleaseYear = year,
                Genre = genre,
                AgeRestriction = age,
                CreatedBy = userId
            };

            var created = AppServices.MediaRepo.Create(item);

            req.Respond((int)HttpStatusCode.Created, MediaToJson(created));
        }

        private void ListMedia(HttpRestEventArgs req)
        {
            var list = AppServices.MediaRepo.GetAll();
            var arr = new JsonArray();

            foreach (var m in list)
            {
                arr.Add(MediaToJson(m));
            }

            var json = new JsonObject
            {
                ["items"] = arr
            };

            req.Respond((int)HttpStatusCode.OK, json);
        }

        private void GetMediaById(HttpRestEventArgs req, int id)
        {
            var item = AppServices.MediaRepo.GetById(id);
            if (item == null)
            {
                SendError(req, HttpStatusCode.NotFound, "Media not found.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, MediaToJson(item));
        }

        private void UpdateMedia(HttpRestEventArgs req, int id, int userId)
        {
            var existing = AppServices.MediaRepo.GetById(id);
            if (existing == null)
            {
                SendError(req, HttpStatusCode.NotFound, "Media not found.");
                return;
            }

            var body = req.Content;

            var title = GetString(body, "Title");
            var mediaType = GetString(body, "MediaType");
            var description = GetString(body, "Description");
            var genre = GetString(body, "Genre");
            var year = GetInt(body, "ReleaseYear");
            var age = GetInt(body, "AgeRestriction");

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(mediaType))
            {
                SendError(req, HttpStatusCode.BadRequest, "Title and MediaType are required.");
                return;
            }

            existing.Title = title!;
            existing.Description = description;
            existing.MediaType = mediaType;
            existing.ReleaseYear = year;
            existing.Genre = genre;
            existing.AgeRestriction = age;

            var ok = AppServices.MediaRepo.Update(existing, userId);
            if (!ok)
            {
                SendError(req, HttpStatusCode.Forbidden, "You are not allowed to update this media.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, MediaToJson(existing));
        }

        private void DeleteMedia(HttpRestEventArgs req, int id, int userId)
        {
            var ok = AppServices.MediaRepo.Delete(id, userId);
            if (!ok)
            {
                SendError(req, HttpStatusCode.Forbidden,
                    "You are not allowed to delete this media or it does not exist.");
                return;
            }

            var json = new JsonObject
            {
                ["success"] = true
            };

            req.Respond((int)HttpStatusCode.OK, json);
        }

        private static JsonObject MediaToJson(MediaItem m)
        {
            var json = new JsonObject
            {
                ["id"] = m.Id,
                ["title"] = m.Title,
                ["description"] = m.Description,
                ["mediaType"] = m.MediaType,
                ["createdBy"] = m.CreatedBy
            };

            if (m.ReleaseYear.HasValue)
                json["releaseYear"] = m.ReleaseYear.Value;

            if (m.Genre != null)
                json["genre"] = m.Genre;

            if (m.AgeRestriction.HasValue)
                json["ageRestriction"] = m.AgeRestriction.Value;

            return json;
        }
    }
}
