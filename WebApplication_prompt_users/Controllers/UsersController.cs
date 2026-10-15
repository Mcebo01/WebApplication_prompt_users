using Microsoft.AspNetCore.Mvc;
using RaceDay_API.Models;          
using RaceDay_API.Security;        // for SessionAuthorize

namespace RaceDay_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public UsersController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/Users/profile
        [HttpGet("profile")]
        [SessionAuthorize]                          // any logged-in user
        public IActionResult GetProfile()
        {
            // TODO: Get current user from session and return profile
            return Ok(new { message = "Profile endpoint - implement later" });
        }

        // PUT: api/Users/profile
        [HttpPut("profile")]
        [SessionAuthorize]
        public IActionResult UpdateProfile([FromBody] object updateDto)
        {
            // TODO: Update first name, last name, phone, etc.
            return Ok(new { message = "Profile updated" });
        }
    }
}