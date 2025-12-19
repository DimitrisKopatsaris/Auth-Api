using Microsoft.AspNetCore.Mvc;
using AuthApi.DTOs;
using AuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly TokenService _tokenService;

        public AuthController(IUserService userService, TokenService tokenService)
        {
            _userService = userService;
            _tokenService = tokenService;
        }

        // ✅ POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var existingUser = await _userService.GetByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest("User with this email already exists.");

            var createdUser = await _userService.RegisterUserAsync(dto);
            return Ok(createdUser); // ⚡ Message handled automatically
        }

        // ✅ POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var token = await _userService.LoginUserAsync(dto);
            if (token == null)
                return Unauthorized("Invalid email or password.");

            return Ok(new { token }); // ⚡ No message here — middleware adds one
        }

        // ✅ GET: api/auth/me
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                Id = userId,
                Email = email,
                Role = role
            });
        }
    }
}
