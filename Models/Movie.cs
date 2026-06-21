using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CemaApp.Models
{
    [Table("Movie")]
    public class Movie
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public string Genre { get; set; } = string.Empty;

        [Range(1, 500, ErrorMessage = "Duration must be between 1 and 500 minutes.")]
        public int DurationMinutes { get; set; }

        public DateTime ReleaseDate { get; set; }

        public string? PosterUrl { get; set; }
        
        public string? TrailerUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
    }
}
