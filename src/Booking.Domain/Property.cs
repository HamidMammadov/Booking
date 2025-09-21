namespace Booking.Domain
{
    public sealed class Property(PropertyId id, string name)
    {
        public PropertyId Id { get; } = id;
        public string Name { get; } = name;
    }
}
