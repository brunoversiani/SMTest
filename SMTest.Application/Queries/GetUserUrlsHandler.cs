using MediatR;
using Microsoft.EntityFrameworkCore;
using SMTest.Application.DTOs;
using SMTest.Infrastructure.Data;

namespace SMTest.Application.Queries
{
    public class GetUserUrlsHandler : IRequestHandler<GetUserUrlsQuery, List<ShortUrlResponse>>
    {
        private readonly AppDbContext _context;

        public GetUserUrlsHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ShortUrlResponse>> Handle(GetUserUrlsQuery request, CancellationToken cancellationToken)
        {
            return await _context.ShortUrls
                .Where(u => u.UserId == request.UserId)
                .OrderByDescending(u => u.HitCount)
                .Select(u => new ShortUrlResponse
                {
                    ShortCode = u.ShortCode,
                    LongUrl = u.LongUrl,
                    HitCount = u.HitCount,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}
