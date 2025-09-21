using Booking.Application.Features.Availability;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers
{
    [ApiController]
    [Route("api/available-homes")]
    public sealed class AvailabilityController(ISender sender, ILogger<AvailabilityController> logger) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(GetAvailableHomesResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GetAvailableHomesResult>> Get(
            [FromQuery] string startDate,
            [FromQuery] string endDate,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            CancellationToken ct)
        {
            if (!DateOnly.TryParseExact(startDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var s) ||
                !DateOnly.TryParseExact(endDate, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var e))
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["dateRange"] = new[] { "Invalid date format. Use yyyy-MM-dd." }
                }));
            }

            var result = await sender.Send(new GetAvailableHomesQuery(s, e, page, pageSize), ct);

            AddPaginationLinks(result, s, e);

            return Ok(result);
        }

        private void AddPaginationLinks(GetAvailableHomesResult result, DateOnly s, DateOnly e)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            string BuildUrl(int targetPage) =>
                $"{baseUrl}?startDate={s:yyyy-MM-dd}&endDate={e:yyyy-MM-dd}&page={targetPage}&pageSize={result.PageSize}";

            var links = new List<string>
            {
                $"<{BuildUrl(1)}>; rel=\"first\"",
                $"<{BuildUrl(result.TotalPages)}>; rel=\"last\""
            };

            if (result.Page > 1)
                links.Add($"<{BuildUrl(result.Page - 1)}>; rel=\"prev\"");
            if (result.Page < result.TotalPages)
                links.Add($"<{BuildUrl(result.Page + 1)}>; rel=\"next\"");

            if (links.Count > 0)
                Response.Headers.Append("Link", string.Join(", ", links));
            Response.Headers.Append("X-Total-Count", result.Total.ToString());
        }
    }
}
