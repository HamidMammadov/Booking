using System.Collections.Immutable;

namespace Booking.Domain.Abstractions
{
    public interface IPropertyReadRepository
    {
        ImmutableDictionary<PropertyId, (Property Property, ImmutableHashSet<DateOnly> Slots)> Snapshot();
    }
}
