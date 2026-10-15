using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay_API.Models
{
    public class Result
    {
        [Key]
        public int ResultId { get; set; }

        [Required]
        public int EnrolmentId { get; set; }

        [Required]
        public TimeSpan FinishTime { get; set; }

        [Required]
        public int Position { get; set; }

        public int? TotalFinishers { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;

        public int? RecordedBy { get; set; }   // Organiser who entered the result

        // Navigation
        [ForeignKey("EnrolmentId")]
        public Enrolment? Enrolment { get; set; }

        [ForeignKey("RecordedBy")]
        public User? Recorder { get; set; }
    }
}