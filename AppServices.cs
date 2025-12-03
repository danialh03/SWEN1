using DhProjekt.Database;

namespace DhProjekt
{
    /// Zentrale Stelle für gemeinsam genutzte Services (DB & Repositories).
    public static class AppServices
    {
        // Eine DB-Connection-Factory
        public static DatabaseConnection Db { get; } = new();

        // Repositories, die darauf aufbauen
        public static UserRepository UserRepo { get; } = new(Db);
        public static MediaRepository MediaRepo { get; } = new(Db);
    }
}
