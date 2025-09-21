using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Booking.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Booking.Tests
{
    public sealed class AvailabilityPagingTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory = factory.WithWebHostBuilder(_ => { });
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static string MakeUrl(DateOnly s, DateOnly e, int? page = null, int? pageSize = null)
        {
            var qs = new List<string>
            {
                $"startDate={s:yyyy-MM-dd}",
                $"endDate={e:yyyy-MM-dd}"
            };
            if (page.HasValue) qs.Add($"page={page.Value}");
            if (pageSize.HasValue) qs.Add($"pageSize={pageSize.Value}");
            return "/api/available-homes?" + string.Join("&", qs);
        }

        private static Dictionary<string, string> ParseLinkHeader(HttpResponseHeaders headers)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!headers.TryGetValues("Link", out var values)) return dict;

            foreach (var value in values)
            {
                var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in parts)
                {
                    var m = Regex.Match(part, @"<(?<url>[^>]+)>\s*;\s*rel=""(?<rel>[^""]+)""");
                    if (m.Success)
                    {
                        dict[m.Groups["rel"].Value] = m.Groups["url"].Value;
                    }
                }
            }
            return dict;
        }

        [Fact]
        public async Task Paging_Returns_Meta_And_Respects_PageSize()
        {
            using var client = _factory.CreateClient();

            var start = new DateOnly(2025, 07, 07);
            var end = new DateOnly(2025, 07, 17);

            var url = MakeUrl(start, end, page: 1, pageSize: 10);
            var resp = await client.GetAsync(url);

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Headers.TryGetValues("X-Total-Count", out var totalHeader).Should().BeTrue("X-Total-Count header missing");

            var payload = await resp.Content.ReadFromJsonAsync<GetAvailableHomesResultDto>(JsonOpts);
            payload.Should().NotBeNull();
            payload!.Status.Should().Be("OK");
            payload.Page.Should().Be(1);
            payload.PageSize.Should().Be(10);
            payload.Total.Should().BeGreaterThan(0);
            payload.TotalPages.Should().BeGreaterThan(0);

            payload.Homes.Should().NotBeNull();
            payload.Homes!.Count.Should().BeLessThanOrEqualTo(10);

            if (payload.Homes.Count > 0)
            {
                var anySlots = payload.Homes.SelectMany(h => h.AvailableSlots ?? Enumerable.Empty<string>()).ToList();
                anySlots.Should().OnlyContain(d => Regex.IsMatch(d, @"^\d{4}-\d{2}-\d{2}$"));
            }

            var links = ParseLinkHeader(resp.Headers);
            links.Should().ContainKey("first");
            links.Should().ContainKey("last");
            if (payload.TotalPages > 1)
            {
                links.Should().ContainKey("next");
                links["first"].Should().Contain("page=1");
                links["next"].Should().Contain("page=2");
            }
        }

        [Fact]
        public async Task Paging_Overflow_Returns_Empty_Homes_But_Correct_Meta()
        {
            using var client = _factory.CreateClient();

            var start = new DateOnly(2025, 07, 07);
            var end = new DateOnly(2025, 07, 17);

            var first = await client.GetFromJsonAsync<GetAvailableHomesResultDto>(MakeUrl(start, end, 1, 5), JsonOpts);
            first.Should().NotBeNull();
            var total = first!.Total;
            var pageSize = 5;
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

            var overflowPage = totalPages + 5;
            var resp = await client.GetAsync(MakeUrl(start, end, overflowPage, pageSize));
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var payload = await resp.Content.ReadFromJsonAsync<GetAvailableHomesResultDto>(JsonOpts);
            payload.Should().NotBeNull();
            payload!.Page.Should().Be(overflowPage);
            payload.PageSize.Should().Be(pageSize);
            payload.Total.Should().Be(total);
            payload.TotalPages.Should().Be(totalPages);
            payload.Homes.Should().NotBeNull();
            payload.Homes!.Count.Should().Be(0);

            var links = ParseLinkHeader(resp.Headers);
            links.Should().ContainKey("first");
            links.Should().ContainKey("last");
        }

        [Fact]
        public async Task PageSize_Caps_To_Max_And_Page_Defaults()
        {
            using var client = _factory.CreateClient();

            var resp = await client.GetAsync(MakeUrl(new(2025, 07, 01), new(2025, 07, 31), null, 5000));
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var payload = await resp.Content.ReadFromJsonAsync<GetAvailableHomesResultDto>(JsonOpts);
            payload.Should().NotBeNull();
            payload!.Page.Should().Be(1);
            payload.PageSize.Should().Be(100);
            payload.Total.Should().BeGreaterThan(0);
            payload.Homes.Should().NotBeNull();
            payload.Homes!.Count.Should().BeLessThanOrEqualTo(100);
        }

        private sealed class GetAvailableHomesResultDto
        {
            public string? Status { get; init; }
            public int Page { get; init; }
            public int PageSize { get; init; }
            public int Total { get; init; }
            public int TotalPages { get; init; }
            public List<HomeDto> Homes { get; init; } = new();

            public sealed class HomeDto
            {
                public string? HomeId { get; init; }
                public string? HomeName { get; init; }
                public List<string>? AvailableSlots { get; init; }
            }
        }
    }
}
