using MediatR;

namespace Booking.Application.Features.Availability
{
    public sealed record GetAvailableHomesQuery(
        DateOnly StartDate,
        DateOnly EndDate,
        int? Page = null,
        int? PageSize = null
    ) : IRequest<GetAvailableHomesResult>;

    public sealed class GetAvailableHomesResult
    {
        public string Status { get; init; } = "OK";
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int Total { get; init; }
        public int TotalPages { get; init; }

        public List<HomeDto> Homes { get; init; } = [];

        public sealed class HomeDto
        {
            public required string HomeId { get; init; }
            public required string HomeName { get; init; }
            public required IEnumerable<DateOnly> AvailableSlots { get; init; }
        }
    }
}