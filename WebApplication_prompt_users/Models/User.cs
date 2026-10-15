using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay_API.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;   // "Organiser" or "Participant"

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(500)]
        public string? ProfileImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ====================== Navigation Properties ======================

        // Events created by this user (when Role = Organiser)
        public ICollection<Event>? OrganisedEvents { get; set; }

        // Enrolments made by this user (when Role = Participant)
        public ICollection<Enrolment>? Enrolments { get; set; }
    }
}