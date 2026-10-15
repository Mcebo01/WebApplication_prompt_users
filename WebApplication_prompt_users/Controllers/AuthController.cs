using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_API.Models;
using RaceDay_API.Security;
using System.ComponentModel.DataAnnotations;

namespace RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(RaceDayContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        // ====================== DTOs ======================
        public class RegisterDto
        {
            [Required]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            public string LastName { get; set; } = string.Empty;

            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, MinLength(6)]
            public string Password { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = string.Empty;

            public string? PhoneNumber { get; set; }
        }

        public class LoginDto
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;
        }

        // ====================== REGISTER ======================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (dto.Role != "Organiser" && dto.Role != "Participant")
            {
                return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
            {
                return Conflict(new { message = "Email is already registered." });
            }

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Role = dto.Role,
                PhoneNumber = dto.PhoneNumber
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
          
            return StatusCode(201, new
            {
                message = "Registration successful",
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role
            });
        }

        // ====================== LOGIN ======================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // Create session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");

            return Ok(new
            {
                message = "Login successful",
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role
            });
        }

        // ====================== LOGOUT ======================
        [HttpPost("logout")]
        [SessionAuthorize]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logged out successfully." });
        }

        // ====================== CURRENT USER ======================
        [HttpGet("me")]
        [SessionAuthorize]
        public IActionResult Me()
        {
            return Ok(new
            {
                userId = HttpContext.Session.GetInt32("UserId"),
                role = HttpContext.Session.GetString("Role"),
                email = HttpContext.Session.GetString("Email"),
                fullName = HttpContext.Session.GetString("FullName")
            });
        }
    }
}