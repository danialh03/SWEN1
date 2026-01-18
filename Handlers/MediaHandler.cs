using System.Net;
using System.Text.Json.Nodes;
using DhProjekt.Auth;
using DhProjekt.Database;
using DhProjekt.Server;

namespace DhProjekt.Handlers
{
    public sealed class MediaHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs req)
        {
            var path = req.Path;
            var method = req.Method;

            if (!path.StartsWith("/api/media"))
                return;

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

            if (path == "/api/media")
            {
                if (method == HttpMethod.Get) { ListMedia(req); return; }
                if (method == HttpMethod.Post) { CreateMedia(req, userId.Value); return; }
            }

            if (path.StartsWith("/api/media/"))
            {
                var idText = path.Substring("/api/media/".Length);
                if (int.TryParse(idText, out var id))
                {
                    if (method == HttpMethod.Get) { GetMediaById(req, id); return; }
                    if (method == HttpMethod.Put) { UpdateMedia(req, id, userId.Value); return; }
                    if (method == HttpMethod.Delete) { DeleteMedia(req, id, userId.Value); return; }
                }
            }
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
            req.Respond((int)HttpStatusCode.Created, MediaToJsonWithStats(created));
        }

        // Search/Filter/Sort via Query Params
        private void ListMedia(HttpRestEventArgs req)
        {
            var q = GetQueryParams(req.Context.Request);

            q.TryGetValue("search", out var search);
            q.TryGetValue("genre", out var genre);
            q.TryGetValue("mediaType", out var mediaType);
            q.TryGetValue("sort", out var sort);
            q.TryGetValue("order", out var order);

            int? year = null;
            if (q.TryGetValue("releaseYear", out var yearText) && int.TryParse(yearText, out var y))
                year = y;

            int? maxAge = null;
            if (q.TryGetValue("maxAgeRestriction", out var ageText) && int.TryParse(ageText, out var a))
                maxAge = a;

            double? minScore = null;
            if (q.TryGetValue("minScore", out var scoreText) && double.TryParse(scoreText, out var s))
                minScore = s;

            var list = AppServices.MediaRepo.GetFiltered(search, genre, mediaType, year, maxAge, minScore, sort, order);

            var arr = new JsonArray();
            foreach (var m in list)
                arr.Add(MediaToJsonWithStats(m));

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["items"] = arr });
        }

        private void GetMediaById(HttpRestEventArgs req, int id)
        {
            var item = AppServices.MediaRepo.GetById(id);
            if (item == null)
            {
                SendError(req, HttpStatusCode.NotFound, "Media not found.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, MediaToJsonWithStats(item));
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

            req.Respond((int)HttpStatusCode.OK, MediaToJsonWithStats(existing));
        }

        private void DeleteMedia(HttpRestEventArgs req, int id, int userId)
        {
            var ok = AppServices.MediaRepo.Delete(id, userId);
            if (!ok)
            {
                SendError(req, HttpStatusCode.Forbidden, "You are not allowed to delete this media or it does not exist.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["success"] = true });
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
