using Microsoft.AspNetCore.Mvc;
using RaceDay_API.Models;
using RaceDay_API.Security;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        // ====================== DTO ======================
        public class EnrolDto
        {
            [Required]
            public int EventId { get; set; }

            [Required]
            public int CategoryId { get; set; }
        }

        // ====================== ENROL (Participant only) ======================
        /// <summary>
        /// Allows a Participant to enrol in an event by selecting a category.
        /// </summary>
        [HttpPost]
        [SessionAuthorize(Roles = "Participant")]
        public async Task<IActionResult> Enrol([FromBody] EnrolDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            // 1. Check if the event exists
            var ev = await _context.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventId == dto.EventId);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            // 2. Check if the category belongs to this event
            var category = ev.Categories?.FirstOrDefault(c => c.CategoryId == dto.CategoryId);
            if (category == null)
                return BadRequest(new { message = "Category does not belong to this event." });

            // 3. Check for duplicate enrolment
            bool alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.ParticipantId == userId && e.EventId == dto.EventId);

            if (alreadyEnrolled)
                return Conflict(new { message = "You are already enrolled in this event." });

            // 4. Create the enrolment
            var enrolment = new Enrolment
            {
                ParticipantId = userId.Value,
                EventId = dto.EventId,
                CategoryId = dto.CategoryId,
                EnrolmentDate = DateTime.Now,
                Status = "Confirmed"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Successfully enrolled",
                enrolmentId = enrolment.EnrolmentId,
                eventId = enrolment.EventId,
                categoryId = enrolment.CategoryId,
                status = enrolment.Status,
                enrolmentDate = enrolment.EnrolmentDate
            });
        }

        // ====================== MY ENROLMENTS (Participant only) ======================
        /// <summary>
        /// Returns all enrolments of the currently logged-in Participant.
        /// </summary>
        [HttpGet("my")]
        [SessionAuthorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantId == userId)
                .Include(e => e.Event)
                .Include(e => e.Category)
                .OrderByDescending(e => e.EnrolmentDate)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.Status,
                    e.EnrolmentDate,
                    Event = new
                    {
                        e.Event!.EventId,
                        e.Event.Name,
                        e.Event.EventDate,
                        e.Event.Location,
                        e.Event.DistanceKm,
                        e.Event.EventType
                    },
                    Category = new
                    {
                        e.Category!.CategoryId,
                        e.Category.Name
                    }
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        // ====================== ENROLMENTS FOR AN EVENT (Organiser only) ======================
        /// <summary>
        /// Returns all enrolments for a specific event. Only the Organiser who owns the event can view them.
        /// </summary>
        [HttpGet("event/{eventId}")]
        [SessionAuthorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            // Check if the event exists and belongs to this organiser
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserId != userId)
                return StatusCode(403, new { message = "You can only view enrolments for events that you created." });

            var enrolments = await _context.Enrolments
                .Where(e => e.EventId == eventId)
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .OrderBy(e => e.EnrolmentDate)
                .Select(e => new
                {
                    e.EnrolmentId,
                    e.Status,
                    e.EnrolmentDate,
                    Participant = new
                    {
                        e.Participant!.UserId,
                        Name = e.Participant.FirstName + " " + e.Participant.LastName,
                        e.Participant.Email
                    },
                    Category = new
                    {
                        e.Category!.CategoryId,
                        e.Category.Name
                    }
                })
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}