using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMTest.Application.DTOs;
using SMTest.Application.Queries;
using SMTest.Application.StableMint.Application.Commands;
using SMTest.Domain.Entities;
using SMTest.Domain.Interfaces;
using SMTest.Infrastructure.Data;

namespace SMTest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrlsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly AppDbContext _context;
        private readonly IRateLimiter _rateLimiter;
        private readonly UserManager<User> _userManager;

        public UrlsController(IMediator mediator, AppDbContext context, IRateLimiter rateLimiter, UserManager<User> userManager)
        {
            _mediator = mediator;
            _context = context;
            _rateLimiter = rateLimiter;
            _userManager = userManager;
        }

        [HttpGet]
        private async Task<User> GetUserAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _userManager.FindByIdAsync(userId);
        }

        [Authorize]
        [HttpPost("shorten")]
        public async Task<IActionResult> Shorten(ShortenRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await GetUserAsync();
            if (user == null) return Unauthorized();

            if (!await _rateLimiter.CanCreateUrl(user))
                return StatusCode(429, "Daily URL creation limit exceeded");

            var code = GenerateShortCode();
            var shortUrl = new ShortUrl
            {
                ShortCode = code,
                LongUrl = request.LongUrl,
                UserId = user.Id
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.ShortUrls.Add(shortUrl);
                await _rateLimiter.RecordUrlCreation(user.Id);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new ShortUrlResponse
                {
                    ShortCode = code,
                    LongUrl = request.LongUrl
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Failed to create short URL");
            }
        }

        [Authorize]
        [HttpGet("urls")]
        public async Task<IActionResult> GetUserUrls()
        {
            var user = await GetUserAsync();
            if (user == null) return Unauthorized();

            var urls = await _context.ShortUrls
            .Where(u => u.UserId == user.Id)
            .OrderByDescending(u => u.HitCount)
            .Select(u => new ShortUrlResponse
            {
                ShortCode = u.ShortCode,
                LongUrl = u.LongUrl,
                HitCount = u.HitCount,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

            return Ok(urls);
        }

        [HttpGet("{shortCode}")]
        public async Task<IActionResult> RedirectTo(string shortCode)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(shortCode))
                return BadRequest("Short code is required");

            // Find the URL
            var url = await _context.ShortUrls
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode);

            if (url == null)
                return NotFound();

            // Rate limiting
            if (!await _rateLimiter.CanAccessUrl(url.ShortCode))
                return StatusCode(429, "Rate limit exceeded");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                url.HitCount++;
                await _rateLimiter.RecordUrlAccess(url.ShortCode);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                Redirect(url.LongUrl);
                return Ok(url); //It does not redirect if on the same statement
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Refresh values from database
                var entry = ex.Entries.Single();
                await entry.ReloadAsync();

                // Try saving again (single retry)
                url.HitCount++;
                try
                {
                    await _context.SaveChangesAsync();
                    return Redirect(url.LongUrl);
                }
                catch
                {
                    // Log and return original URL if still failing
                    //_logger.LogError(ex, "Concurrency conflict updating hit count for {ShortCode}", shortCode);
                    return Redirect(url.LongUrl);
                }
            }
        }

        [Authorize]
        [HttpDelete("urls/{shortCode}")]
        public async Task<IActionResult> DeleteShortCode(string shortCode)
        {
            var user = await GetUserAsync();
            if (user == null) return Unauthorized();

            var url = await _context.ShortUrls
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode && u.UserId == user.Id);

            if (url == null) return NotFound();

            _context.ShortUrls.Remove(url);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static string GenerateShortCode()
        {
            return Guid.NewGuid().ToString("N")[..6];
        }
    }
}
