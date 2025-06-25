using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMTest.Application.DTOs;
using SMTest.Domain.Entities;
using SMTest.Domain.Interfaces;

namespace SMTest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IAuthService _authService;
        private readonly SignInManager<User> _signInManager;

        public AuthController(
            UserManager<User> userManager,
            IAuthService authService,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _authService = authService;
            _signInManager = signInManager;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _userManager.Users.AnyAsync(x => x.Email == request.Email))
                return Conflict(new { Message = "Email already registered" });

            var user = new User
            {
                Email = request.Email,
                UserName = request.Email  // Using email as username
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new
                {
                    Message = "Registration failed",
                    Errors = createResult.Errors.Select(e => e.Description)
                });
            }

            return Ok(new { Message = "Registration successful" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _signInManager.PasswordSignInAsync(
                userName: request.Email,  // Email as username
                password: request.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (!result.Succeeded)
                return Unauthorized(new { Message = "Invalid credentials" });

            // Get the user (now that we know credentials are correct)
            var user = await _userManager.FindByEmailAsync(request.Email);
            var token = _authService.CreateToken(user);

            return Ok(new
            {
                Token = token,
                Email = user.Email,
                Message = "Login successful"
            });
        }

       
    }

}
