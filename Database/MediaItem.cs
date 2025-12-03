namespace DhProjekt.Database
{
    public class MediaItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? MediaType { get; set; }  // movie / series / game

        public int? ReleaseYear { get; set; }

        public string? Genre { get; set; }

        public int? AgeRestriction { get; set; }

        public int CreatedBy { get; set; }
    }
}
