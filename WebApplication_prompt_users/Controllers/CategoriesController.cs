using Microsoft.AspNetCore.Mvc;
using RaceDay_API.Models;
using RaceDay_API.Security;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        // ====================== DTOs ======================
        public class CreateCategoryDto
        {
            [Required]
            public int EventId { get; set; }

            [Required, MaxLength(50)]
            public string Name { get; set; } = string.Empty;

            [MaxLength(200)]
            public string? Description { get; set; }

            public int? MinAge { get; set; }
            public int? MaxAge { get; set; }

            [MaxLength(10)]
            public string? GenderRestriction { get; set; } // Male, Female, Open

            public int? MaxParticipants { get; set; }
        }

        public class UpdateCategoryDto
        {
            [MaxLength(50)]
            public string? Name { get; set; }

            [MaxLength(200)]
            public string? Description { get; set; }

            public int? MinAge { get; set; }
            public int? MaxAge { get; set; }

            [MaxLength(10)]
            public string? GenderRestriction { get; set; }

            public int? MaxParticipants { get; set; }
        }

        // ====================== GET CATEGORIES (optionally filtered by event) ======================
        /// <summary>
        /// Returns categories. Optionally filter by eventId.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories([FromQuery] int? eventId)
        {
            var query = _context.Categories.AsQueryable();

            if (eventId.HasValue)
            {
                query = query.Where(c => c.EventId == eventId.Value);
            }

            var categories = await query
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.CategoryId,
                    c.EventId,
                    c.Name,
                    c.Description,
                    c.MinAge,
                    c.MaxAge,
                    c.GenderRestriction,
                    c.MaxParticipants
                })
                .ToListAsync();

            return Ok(categories);
        }

        // ====================== GET SINGLE CATEGORY ======================
        /// <summary>
        /// Returns a single category by ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            return Ok(new
            {
                category.CategoryId,
                category.EventId,
                category.Name,
                category.Description,
                category.MinAge,
                category.MaxAge,
                category.GenderRestriction,
                category.MaxParticipants
            });
        }

        // ====================== CREATE CATEGORY (Organiser only) ======================
        /// <summary>
        /// Creates a new category for an event. Only the Organiser who owns the event can create it.
        /// </summary>
        [HttpPost]
        [SessionAuthorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            // Check if the event exists and belongs to this organiser
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == dto.EventId);
            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserId != userId)
                return StatusCode(403, new { message = "You can only add categories to events that you created." });

            // Optional validation for gender
            if (!string.IsNullOrEmpty(dto.GenderRestriction))
            {
                var allowed = new[] { "Male", "Female", "Open" };
                if (!allowed.Contains(dto.GenderRestriction, StringComparer.OrdinalIgnoreCase))
                    return BadRequest(new { message = "GenderRestriction must be Male, Female or Open." });
            }

            var category = new Category
            {
                EventId = dto.EventId,
                Name = dto.Name,
                Description = dto.Description,
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge,
                GenderRestriction = dto.GenderRestriction,
                MaxParticipants = dto.MaxParticipants
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Category created successfully",
                categoryId = category.CategoryId,
                category.EventId,
                category.Name,
                category.MinAge,
                category.MaxAge,
                category.GenderRestriction,
                category.MaxParticipants
            });
        }

        // ====================== UPDATE CATEGORY (Organiser only) ======================
        /// <summary>
        /// Updates an existing category. Only the Organiser who owns the related event can update it.
        /// </summary>
        [HttpPut("{id}")]
        [SessionAuthorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null || category.Event.OrganiserId != userId)
                return StatusCode(403, new { message = "You can only update categories of events that you created." });

            if (!string.IsNullOrWhiteSpace(dto.Name))
                category.Name = dto.Name;

            if (dto.Description != null)
                category.Description = dto.Description;

            if (dto.MinAge.HasValue)
                category.MinAge = dto.MinAge;

            if (dto.MaxAge.HasValue)
                category.MaxAge = dto.MaxAge;

            if (dto.GenderRestriction != null)
            {
                var allowed = new[] { "Male", "Female", "Open" };
                if (!string.IsNullOrEmpty(dto.GenderRestriction) &&
                    !allowed.Contains(dto.GenderRestriction, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "GenderRestriction must be Male, Female or Open." });
                }
                category.GenderRestriction = dto.GenderRestriction;
            }

            if (dto.MaxParticipants.HasValue)
                category.MaxParticipants = dto.MaxParticipants;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Category updated successfully",
                category.CategoryId,
                category.Name,
                category.MinAge,
                category.MaxAge,
                category.GenderRestriction,
                category.MaxParticipants
            });
        }

        // ====================== DELETE CATEGORY (Organiser only) ======================
        /// <summary>
        /// Deletes a category. Only the Organiser who owns the event can delete it.
        /// </summary>
        [HttpDelete("{id}")]
        [SessionAuthorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var category = await _context.Categories
                .Include(c => c.Event)
                .Include(c => c.Enrolments)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event == null || category.Event.OrganiserId != userId)
                return StatusCode(403, new { message = "You can only delete categories of events that you created." });

            // Prevent delete if there are enrolments using this category
            if (category.Enrolments != null && category.Enrolments.Any())
            {
                return Conflict(new { message = "Cannot delete a category that already has enrolments." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }
    }
}