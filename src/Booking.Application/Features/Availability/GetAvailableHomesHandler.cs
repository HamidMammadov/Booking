using Booking.Application.Common.Pagination;
using Booking.Application.Utility;
using Booking.Domain.Abstractions;
using MediatR;
using System.Collections.Immutable;

namespace Booking.Application.Features.Availability
{
    public sealed class GetAvailableHomesHandler(IPropertyReadRepository repo) : IRequestHandler<GetAvailableHomesQuery, GetAvailableHomesResult>
    {
        public Task<GetAvailableHomesResult> Handle(GetAvailableHomesQuery request, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                var snapshot = repo.Snapshot();
                var range = DateRange.Closed(request.StartDate, request.EndDate).ToArray();
                var rangeSet = range.ToImmutableHashSet();

                var matches = new List<GetAvailableHomesResult.HomeDto>(capacity: Math.Min(1024, snapshot.Count));
                foreach (var kv in snapshot)
                {
                    ct.ThrowIfCancellationRequested();
                    var (property, slots) = kv.Value;

                    var intersect = slots.Where(rangeSet.Contains).OrderBy(d => d).ToArray();
                    if (intersect.Length == 0) continue;

                    matches.Add(new GetAvailableHomesResult.HomeDto
                    {
                        HomeId = property.Id.Value,
                        HomeName = property.Name,
                        AvailableSlots = intersect
                    });
                }

                var ordered = matches
                    .OrderBy(h => h.HomeName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(h => h.HomeId, StringComparer.Ordinal);

                var (page, size) = Paging.Normalize(request.Page, request.PageSize);
                var total = matches.Count;
                var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)size);

                var items = (page - 1) * size >= total
                    ? Enumerable.Empty<GetAvailableHomesResult.HomeDto>()
                    : ordered.Skip((page - 1) * size).Take(size).ToList();

                return new GetAvailableHomesResult
                {
                    Status = "OK",
                    Page = page,
                    PageSize = size,
                    Total = total,
                    TotalPages = totalPages,
                    Homes = items.ToList()
                };
            }, ct);
        }
    }
}
