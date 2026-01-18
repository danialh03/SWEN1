using DhProjekt.Database;

namespace DhProjekt
{
    /// Zentrale Stelle für gemeinsam genutzte Services (DB & Repositories).
    public static class AppServices
    {
        // Eine DB-Connection-Factory
        public static DatabaseConnection Db { get; } = new();

        // Repositories
        public static UserRepository UserRepo { get; } = new(Db);
        public static MediaRepository MediaRepo { get; } = new(Db);

        // Sessions (Tokens) in der DB
        public static SessionRepository SessionRepo { get; } = new(Db);

        // Ratings + Likes + Stats
        public static RatingRepository RatingRepo { get; } = new(Db);

        // Favorites
        public static FavoriteRepository FavoriteRepo { get; } = new(Db);

        // Recommendations
        public static RecommendationRepository RecommendationRepo { get; } = new(Db);
    }
}
