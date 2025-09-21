namespace Booking.Domain
{
    public readonly record struct PropertyId(string Value)
    {
        public override string ToString() => Value;
    }
}
