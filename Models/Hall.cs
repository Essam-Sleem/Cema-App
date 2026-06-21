using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CemaApp.Models
{
    [Table("Hall")]

    public class Hall
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(1, 26, ErrorMessage = "Total rows must be between 1 and 26.")]
        public int TotalRows { get; set; }

        [Range(1, 100, ErrorMessage = "Seats per row must be between 1 and 100.")]
        public int SeatsPerRow { get; set; }

        public ICollection<Screening> Screenings { get; set; } = new List<Screening>();
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
