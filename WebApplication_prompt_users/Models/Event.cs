using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay_API.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateOnly EventDate { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal DistanceKm { get; set; }

        [Required]
        [MaxLength(30)]
        public string EventType { get; set; } = string.Empty;   // Run, Walk, Cycle

        [MaxLength(500)]
        public string? BannerImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign Key - Organiser
        [Required]
        public int OrganiserId { get; set; }

        [ForeignKey("OrganiserId")]
        public User? Organiser { get; set; }

        // Navigation
        public ICollection<Category>? Categories { get; set; }
        public ICollection<Enrolment>? Enrolments { get; set; }
    }
}