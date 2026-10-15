using Microsoft.AspNetCore.Mvc;
using RaceDay_API.Models;
using RaceDay_API.Security;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        // ====================== DTOs ======================
        public class CreateEventDto
        {
            [Required, MaxLength(100)]
            public string Name { get; set; } = string.Empty;

            [MaxLength(500)]
            public string? Description { get; set; }

            [Required]
            public DateOnly EventDate { get; set; }

            [Required, MaxLength(200)]
            public string Location { get; set; } = string.Empty;

            [Required]
            [Range(0.1, 999.99)]
            public decimal DistanceKm { get; set; }

            [Required, MaxLength(30)]
            public string EventType { get; set; } = string.Empty; // Run, Walk, Cycle
        }

        public class UpdateEventDto
        {
            [MaxLength(100)]
            public string? Name { get; set; }

            [MaxLength(500)]
            public string? Description { get; set; }

            public DateOnly? EventDate { get; set; }

            [MaxLength(200)]
            public string? Location { get; set; }

            [Range(0.1, 999.99)]
            public decimal? DistanceKm { get; set; }

            [MaxLength(30)]
            public string? EventType { get; set; }
        }

        // ====================== GET ALL EVENTS (Public) ======================
        /// <summary>
        /// Returns a list of all events. Public endpoint.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Include(e => e.Organiser)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventId,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.DistanceKm,
                    e.EventType,
                    e.BannerImageUrl,
                    Organiser = e.Organiser != null ? e.Organiser.FirstName + " " + e.Organiser.LastName : null
                })
                .ToListAsync();

            return Ok(events);
        }

        // ====================== GET SINGLE EVENT ======================
        /// <summary>
        /// Returns a single event by ID including its categories.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Organiser)
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            return Ok(new
            {
                ev.EventId,
                ev.Name,
                ev.Description,
                ev.EventDate,
                ev.Location,
                ev.DistanceKm,
                ev.EventType,
                ev.BannerImageUrl,
                Organiser = ev.Organiser != null ? new
                {
                    ev.Organiser.UserId,
                    Name = ev.Organiser.FirstName + " " + ev.Organiser.LastName
                } : null,
                Categories = ev.Categories?.Select(c => new
                {
                    c.CategoryId,
                    c.Name,
                    c.Description,
                    c.MinAge,
                    c.MaxAge,
                    c.GenderRestriction,
                    c.MaxParticipants
                })
            });
        }

        // ====================== GET MY EVENTS (Organiser only) ======================
        /// <summary>
        /// Returns all events created by the currently logged-in Organiser.
        /// </summary>
        [HttpGet("my")]
        [SessionAuthorize(Roles = "Organiser")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var events = await _context.Events
                .Where(e => e.OrganiserId == userId)
                .Include(e => e.Categories)
                .OrderByDescending(e => e.EventDate)
                .Select(e => new
                {
                    e.EventId,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.DistanceKm,
                    e.EventType,
                    e.BannerImageUrl,
                    CategoryCount = e.Categories != null ? e.Categories.Count : 0
                })
                .ToListAsync();

            return Ok(events);
        }

        // ====================== CREATE EVENT (Organiser only) ======================
        /// <summary>
        /// Creates a new event. Only Organisers can call this endpoint.
        /// </summary>
        [HttpPost]
        [SessionAuthorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            // Basic validation for EventType
            var allowedTypes = new[] { "Run", "Walk", "Cycle" };
            if (!allowedTypes.Contains(dto.EventType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });
            }

            var newEvent = new Event
            {
                Name = dto.Name,
                Description = dto.Description,
                EventDate = dto.EventDate,
                Location = dto.Location,
                DistanceKm = dto.DistanceKm,
                EventType = dto.EventType,
                OrganiserId = userId.Value,
                CreatedAt = DateTime.Now
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Event created successfully",
                eventId = newEvent.EventId,
                newEvent.Name,
                newEvent.EventDate,
                newEvent.Location,
                newEvent.DistanceKm,
                newEvent.EventType
            });
        }

        // ====================== UPDATE EVENT (Organiser only - own events) ======================
        /// <summary>
        /// Updates an existing event. Only the Organiser who created it can update it.
        /// </summary>
        [HttpPut("{id}")]
        [SessionAuthorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null)
                return NotFound(new { message = "Event not found." });

            // Only the owner can update
            if (ev.OrganiserId != userId)
                return StatusCode(403, new { message = "You can only update events that you created." });

            // Update only the fields that were sent
            if (!string.IsNullOrWhiteSpace(dto.Name))
                ev.Name = dto.Name;

            if (dto.Description != null)
                ev.Description = dto.Description;

            if (dto.EventDate.HasValue)
                ev.EventDate = dto.EventDate.Value;

            if (!string.IsNullOrWhiteSpace(dto.Location))
                ev.Location = dto.Location;

            if (dto.DistanceKm.HasValue)
                ev.DistanceKm = dto.DistanceKm.Value;

            if (!string.IsNullOrWhiteSpace(dto.EventType))
            {
                var allowedTypes = new[] { "Run", "Walk", "Cycle" };
                if (!allowedTypes.Contains(dto.EventType, StringComparer.OrdinalIgnoreCase))
                    return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

                ev.EventType = dto.EventType;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event updated successfully",
                ev.EventId,
                ev.Name,
                ev.EventDate,
                ev.Location,
                ev.DistanceKm,
                ev.EventType
            });
        }

        // ====================== DELETE EVENT (Organiser only - own events) ======================
        /// <summary>
        /// Deletes an event. Only the Organiser who created it can delete it.
        /// </summary>
        [HttpDelete("{id}")]
        [SessionAuthorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var ev = await _context.Events
                .Include(e => e.Categories)
                .Include(e => e.Enrolments)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserId != userId)
                return StatusCode(403, new { message = "You can only delete events that you created." });

            // Optional: prevent delete if there are enrolments
            if (ev.Enrolments != null && ev.Enrolments.Any())
            {
                return Conflict(new { message = "Cannot delete an event that already has enrolments." });
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }
    }
}