using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthApi.Services;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // ✅ GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        // ✅ GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        // ✅ DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _userService.DeleteUserAsync(id);
            if (!deleted)
                return NotFound();

            // ⚡ No need to return a custom string — middleware adds message
            return Ok();
        }

        // ✅ PUT: api/users/{id}/promote
        [HttpPut("{id}/promote")]
        public async Task<IActionResult> Promote(Guid id)
        {
            var promoted = await _userService.PromoteUserAsync(id);
            if (!promoted)
                return NotFound();

            return Ok();
        }

        // ✅ PUT: api/users/{id}/demote
        [HttpPut("{id}/demote")]
        public async Task<IActionResult> Demote(Guid id)
        {
            var demoted = await _userService.DemoteUserAsync(id);
            if (!demoted)
                return NotFound();

            return Ok();
        }
    }
}
