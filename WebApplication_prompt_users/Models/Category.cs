using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay_API.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;   // e.g. "10km", "Under 20", "Senior"

        [MaxLength(200)]
        public string? Description { get; set; }

        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }

        [MaxLength(10)]
        public string? GenderRestriction { get; set; }   // Male, Female, Open

        public int? MaxParticipants { get; set; }

        // Foreign Key
        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        // Navigation
        public ICollection<Enrolment>? Enrolments { get; set; }
    }
}