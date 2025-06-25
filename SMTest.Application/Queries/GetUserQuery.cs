using MediatR;
using SMTest.Application.DTOs;

namespace SMTest.Application.Queries
{
    public class GetUserUrlsQuery : IRequest<List<ShortUrlResponse>>
    {
        public string UserId { get; set; }
    }

}
