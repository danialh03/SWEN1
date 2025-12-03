using DhProjekt.Server;

namespace DhProjekt.Handlers
{
    /// Alle Request-Handler implementieren dieses Interface
    public interface IHandler
    {
        /// Versucht, den Request zu behandeln
        void Handle(HttpRestEventArgs e);
    }
}
