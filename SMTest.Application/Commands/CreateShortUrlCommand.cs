using MediatR;
using SMTest.Application.DTOs;
using SMTest.Domain.Entities;

namespace SMTest.Application
{
    namespace StableMint.Application.Commands
    {
        public class CreateShortUrlCommand : IRequest<ShortUrlResponse>
        {
            public string LongUrl { get; set; }
            public User User { get; set; }
        }
    }
}
