using Microsoft.AspNetCore.Mvc;
using RaceDay_API.Models;
using RaceDay_API.Security;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        // ====================== DTO ======================
        public class CaptureResultDto
        {
            [Required]
            public int EnrolmentId { get; set; }

            [Required]
            public string FinishTime { get; set; } = string.Empty; // e.g. "01:45:30" or "1:45:30"

            [Required]
            [Range(1, int.MaxValue)]
            public int Position { get; set; }

            [Range(1, int.MaxValue)]
            public int? TotalFinishers { get; set; }
        }

        // ====================== CAPTURE RESULT (Organiser only) ======================
        /// <summary>
        /// Organiser captures finish time and position for a participant enrolment.
        /// </summary>
        [HttpPost]
        [SessionAuthorize(Roles = "Organiser")]
        public async Task<IActionResult> CaptureResult([FromBody] CaptureResultDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            // 1. Find the enrolment
            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            // 2. Only the organiser of that event can capture results
            if (enrolment.Event == null || enrolment.Event.OrganiserId != userId)
                return StatusCode(403, new { message = "You can only capture results for events that you organised." });

            // 3. Check if a result already exists for this enrolment
            bool resultExists = await _context.Results
                .AnyAsync(r => r.EnrolmentId == dto.EnrolmentId);

            if (resultExists)
                return Conflict(new { message = "A result has already been recorded for this enrolment." });

            // 4. Parse FinishTime
            if (!TimeSpan.TryParse(dto.FinishTime, out TimeSpan finishTime))
            {
                return BadRequest(new { message = "FinishTime must be in a valid format (e.g. 01:45:30)." });
            }

            // 5. Create the result
            var result = new Result
            {
                EnrolmentId = dto.EnrolmentId,
                FinishTime = finishTime,
                Position = dto.Position,
                TotalFinishers = dto.TotalFinishers,
                RecordedAt = DateTime.Now,
                RecordedBy = userId
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Result captured successfully",
                resultId = result.ResultId,
                enrolmentId = result.EnrolmentId,
                finishTime = result.FinishTime.ToString(@"hh\:mm\:ss"),
                position = result.Position,
                totalFinishers = result.TotalFinishers
            });
        }

        // ====================== MY RESULTS (Participant only) ======================
        /// <summary>
        /// Returns the race history (results) of the currently logged-in Participant.
        /// </summary>
        [HttpGet("my")]
        [SessionAuthorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var results = await _context.Results
                .Include(r => r.Enrolment)!
                    .ThenInclude(e => e!.Event)
                .Include(r => r.Enrolment)!
                    .ThenInclude(e => e!.Category)
                .Where(r => r.Enrolment!.ParticipantId == userId)
                .OrderByDescending(r => r.Enrolment!.Event!.EventDate)
                .Select(r => new
                {
                    r.ResultId,
                    FinishTime = r.FinishTime.ToString(@"hh\:mm\:ss"),
                    r.Position,
                    r.TotalFinishers,
                    PositionText = r.TotalFinishers.HasValue
                        ? $"{r.Position}th out of {r.TotalFinishers}"
                        : r.Position.ToString(),
                    Event = new
                    {
                        r.Enrolment!.Event!.EventId,
                        r.Enrolment.Event.Name,
                        r.Enrolment.Event.EventDate,
                        r.Enrolment.Event.Location,
                        r.Enrolment.Event.EventType
                    },
                    Category = new
                    {
                        r.Enrolment.Category!.CategoryId,
                        r.Enrolment.Category.Name
                    }
                })
                .ToListAsync();

            return Ok(results);
        }

        // ====================== RESULTS FOR AN EVENT ======================
        /// <summary>
        /// Returns all results for a specific event (public leaderboard style).
        /// </summary>
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetResultsForEvent(int eventId)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null)
                return NotFound(new { message = "Event not found." });

            var results = await _context.Results
                .Include(r => r.Enrolment)!
                    .ThenInclude(e => e!.Participant)
                .Include(r => r.Enrolment)!
                    .ThenInclude(e => e!.Category)
                .Where(r => r.Enrolment!.EventId == eventId)
                .OrderBy(r => r.Position)
                .Select(r => new
                {
                    r.ResultId,
                    FinishTime = r.FinishTime.ToString(@"hh\:mm\:ss"),
                    r.Position,
                    r.TotalFinishers,
                    Participant = new
                    {
                        r.Enrolment!.Participant!.UserId,
                        Name = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName
                    },
                    Category = new
                    {
                        r.Enrolment.Category!.CategoryId,
                        r.Enrolment.Category.Name
                    }
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}