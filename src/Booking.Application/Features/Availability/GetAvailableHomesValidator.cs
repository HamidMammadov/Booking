using Booking.Application.Common.Pagination;
using FluentValidation;

namespace Booking.Application.Features.Availability
{
    public sealed class GetAvailableHomesValidator : AbstractValidator<GetAvailableHomesQuery>
    {
        public GetAvailableHomesValidator()
        {
            RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);

            RuleFor(x => x).Must(x => x.StartDate.AddDays(60) >= x.EndDate)
                .WithMessage("Date range too large. Max 60 days.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .When(x => x.Page.HasValue);

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .When(x => x.PageSize.HasValue);
        }
    }
}
