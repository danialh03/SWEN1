using System.Net;
using System.Text.Json.Nodes;
using DhProjekt.Auth;
using DhProjekt.Database;
using DhProjekt.Server;

namespace DhProjekt.Handlers
{
    public sealed class RatingHandler : Handler, IHandler
    {
        public override void Handle(HttpRestEventArgs req)
        {
            var path = req.Path;
            var method = req.Method;

            // ich kümmer mich um /api/media/{id}/ratings und /api/ratings/...
            if (!path.StartsWith("/api/media") && !path.StartsWith("/api/ratings"))
                return;

            // Alle Rating-Routen brauchen Login
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

            // /api/media/{mediaId}/ratings
            // POST: Rating erstellen
            // GET: Ratings lesen
            if (path.StartsWith("/api/media/") && path.EndsWith("/ratings"))
            {
                var middle = path.Substring("/api/media/".Length);
                // middle z.B. "12/ratings"
                var mediaIdText = middle.Replace("/ratings", "");
                if (!int.TryParse(mediaIdText, out var mediaId))
                    return;

                if (method == HttpMethod.Post)
                {
                    CreateRating(req, mediaId, userId.Value);
                    return;
                }

                if (method == HttpMethod.Get)
                {
                    ListRatings(req, mediaId, userId.Value);
                    return;
                }
            }

            // 2) /api/ratings/{ratingId}
            if (path.StartsWith("/api/ratings/"))
            {
                var rest = path.Substring("/api/ratings/".Length);

                // like/unlike/confirm sind Unterpfade
                if (rest.EndsWith("/confirm"))
                {
                    var idText = rest.Replace("/confirm", "");
                    if (int.TryParse(idText, out var ratingId) && method == HttpMethod.Post)
                    {
                        Confirm(req, ratingId, userId.Value);
                        return;
                    }
                }

                if (rest.EndsWith("/like"))
                {
                    var idText = rest.Replace("/like", "");
                    if (int.TryParse(idText, out var ratingId))
                    {
                        if (method == HttpMethod.Post)
                        {
                            Like(req, ratingId, userId.Value);
                            return;
                        }
                        if (method == HttpMethod.Delete)
                        {
                            Unlike(req, ratingId, userId.Value);
                            return;
                        }
                    }
                }

                // normaler ratingId-Pfad
                if (int.TryParse(rest, out var id))
                {
                    if (method == HttpMethod.Put)
                    {
                        UpdateRating(req, id, userId.Value);
                        return;
                    }

                    if (method == HttpMethod.Delete)
                    {
                        DeleteRating(req, id, userId.Value);
                        return;
                    }
                }
            }
        }

        private void CreateRating(HttpRestEventArgs req, int mediaId, int userId)
        {
            // Media existiert?
            var media = AppServices.MediaRepo.GetById(mediaId);
            if (media == null)
            {
                SendError(req, HttpStatusCode.NotFound, "Media not found.");
                return;
            }

            var body = req.Content;

            var stars = GetInt(body, "Stars");
            var comment = GetString(body, "Comment");

            if (stars == null || stars < 1 || stars > 5)
            {
                SendError(req, HttpStatusCode.BadRequest, "Stars must be between 1 and 5.");
                return;
            }

            var created = AppServices.RatingRepo.Create(mediaId, userId, stars.Value, comment);

            if (created == null)
            {
                // meistens: unique(user_id, media_id) -> user hat schon bewertet
                SendError(req, HttpStatusCode.Conflict, "You already rated this media.");
                return;
            }

            req.Respond((int)HttpStatusCode.Created, RatingToJson(created, likeCount: 0, requesterUserId: userId));
        }

        private void ListRatings(HttpRestEventArgs req, int mediaId, int requesterUserId)
        {
            var media = AppServices.MediaRepo.GetById(mediaId);
            if (media == null)
            {
                SendError(req, HttpStatusCode.NotFound, "Media not found.");
                return;
            }

            var list = AppServices.RatingRepo.GetByMedia(mediaId);

            var arr = new JsonArray();
            foreach (var (rating, likeCount) in list)
            {
                arr.Add(RatingToJson(rating, likeCount, requesterUserId));
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject
            {
                ["items"] = arr
            });
        }

        private void UpdateRating(HttpRestEventArgs req, int ratingId, int userId)
        {
            var existing = AppServices.RatingRepo.GetById(ratingId);
            if (existing == null)
            {
                SendError(req, HttpStatusCode.NotFound, "Rating not found.");
                return;
            }

            var body = req.Content;
            var stars = GetInt(body, "Stars");
            var comment = GetString(body, "Comment");

            if (stars == null || stars < 1 || stars > 5)
            {
                SendError(req, HttpStatusCode.BadRequest, "Stars must be between 1 and 5.");
                return;
            }

            var ok = AppServices.RatingRepo.Update(ratingId, userId, stars.Value, comment);
            if (!ok)
            {
                SendError(req, HttpStatusCode.Forbidden, "You are not allowed to update this rating.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }

        private void DeleteRating(HttpRestEventArgs req, int ratingId, int userId)
        {
            var ok = AppServices.RatingRepo.Delete(ratingId, userId);
            if (!ok)
            {
                SendError(req, HttpStatusCode.Forbidden, "You are not allowed to delete this rating or it does not exist.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }

        private void Confirm(HttpRestEventArgs req, int ratingId, int userId)
        {
            var ok = AppServices.RatingRepo.ConfirmComment(ratingId, userId);
            if (!ok)
            {
                SendError(req, HttpStatusCode.Forbidden, "You are not allowed to confirm this rating or it does not exist.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }

        private void Like(HttpRestEventArgs req, int ratingId, int userId)
        {
            // rating existiert?
            var existing = AppServices.RatingRepo.GetById(ratingId);
            if (existing == null)
            {
                SendError(req, HttpStatusCode.NotFound, "Rating not found.");
                return;
            }

            var ok = AppServices.RatingRepo.Like(ratingId, userId);
            if (!ok)
            {
                SendError(req, HttpStatusCode.Conflict, "You already liked this rating.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }

        private void Unlike(HttpRestEventArgs req, int ratingId, int userId)
        {
            var ok = AppServices.RatingRepo.Unlike(ratingId, userId);
            if (!ok)
            {
                SendError(req, HttpStatusCode.NotFound, "Like not found.");
                return;
            }

            req.Respond((int)HttpStatusCode.OK, new JsonObject { ["success"] = true });
        }

        private static JsonObject RatingToJson(Rating r, int likeCount, int requesterUserId)
        {
            // Kommentar-Regel:
            // - öffentlich nur wenn bestätigt
            // - Autor sieht eigenen Kommentar immer
            string? comment = null;
            if (r.CommentConfirmed || r.UserId == requesterUserId)
                comment = r.Comment;

            var json = new JsonObject
            {
                ["id"] = r.Id,
                ["mediaId"] = r.MediaId,
                ["userId"] = r.UserId,
                ["stars"] = r.Stars,
                ["commentConfirmed"] = r.CommentConfirmed,
                ["likeCount"] = likeCount,
                ["createdAt"] = r.CreatedAt,
                ["updatedAt"] = r.UpdatedAt
            };

            if (comment != null)
                json["comment"] = comment;

            return json;
        }
    }
}
