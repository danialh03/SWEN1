namespace DhProjekt.Database
{
    public class Rating
    {
        public int Id { get; set; }

        public int MediaId { get; set; }
        public int UserId { get; set; }

        public int Stars { get; set; }             // 1..5
        public string? Comment { get; set; }       // optional

        public bool CommentConfirmed { get; set; } // Kommentar erst öffentlich wenn true

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
