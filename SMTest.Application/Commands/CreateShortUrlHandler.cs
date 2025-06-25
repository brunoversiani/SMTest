using MediatR;
using SMTest.Application.DTOs;
using SMTest.Application.StableMint.Application.Commands;
using SMTest.Domain.Entities;
using SMTest.Domain.Interfaces;
using SMTest.Infrastructure.Data;

namespace SMTest.Application.Commands
{
    public class CreateShortUrlHandler : IRequestHandler<CreateShortUrlCommand, ShortUrlResponse>
    {
        private readonly AppDbContext _context;
        private readonly IRateLimiter _rateLimiter;

        public CreateShortUrlHandler(AppDbContext context, IRateLimiter rateLimiter)
        {
            _context = context;
            _rateLimiter = rateLimiter;
        }

        public async Task<ShortUrlResponse> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
        {
            if (request.User == null)
                throw new ArgumentNullException(nameof(request.User), "User cannot be null");

            var code = GenerateShortCode();
            var shortUrl = new ShortUrl
            {
                ShortCode = code,
                LongUrl = request.LongUrl,
                UserId = request.User.Id
            };

            await _context.ShortUrls.AddAsync(shortUrl, cancellationToken);
            await _rateLimiter.RecordUrlCreation(request.User.Id);
            await _context.SaveChangesAsync(cancellationToken);

            return new ShortUrlResponse
            {
                ShortCode = code,
                LongUrl = request.LongUrl
            };
        }

        private static string GenerateShortCode()
        {
            return Guid.NewGuid().ToString("N")[..6];
        }
    }
}
