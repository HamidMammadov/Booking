using System.Collections.Immutable;
using System.Globalization;
using Booking.Domain;
using Booking.Domain.Abstractions;
using Booking.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Booking.Infrastructure.Repositories
{
    public sealed class PropertyReadRepository(IOptions<DataSeedOptions> options) : IPropertyReadRepository
    {
        private readonly ImmutableDictionary<PropertyId, (Property Property, ImmutableHashSet<DateOnly> Slots)> _data = Seed(options.Value);

        public ImmutableDictionary<PropertyId, (Property Property, ImmutableHashSet<DateOnly> Slots)> Snapshot() => _data;

        private static ImmutableDictionary<PropertyId, (Property, ImmutableHashSet<DateOnly>)> Seed(DataSeedOptions cfg)
        {
            var dict = ImmutableDictionary.CreateBuilder<PropertyId, (Property, ImmutableHashSet<DateOnly>)>();
            var rng = new Random(cfg.RandomSeed ?? Environment.TickCount);

            var start = cfg.SeedStart;
            var end = cfg.SeedEnd;
            if (end < start) (start, end) = (end, start);
            int totalDays = end.DayNumber - start.DayNumber + 1;

            string GenName(int i)
            {
                var city = cfg.Locales[rng.Next(cfg.Locales.Length)];
                var adj = cfg.NameAdjectives[rng.Next(cfg.NameAdjectives.Length)];
                var form = cfg.NameShapes[rng.Next(cfg.NameShapes.Length)];
                return $"{city} {adj} {form} #{1000 + i}";
            }

            HashSet<DateOnly> GenerateSlots()
            {
                var slots = new HashSet<DateOnly>();

                int blocks = rng.Next(cfg.BlocksPerProperty.Min, cfg.BlocksPerProperty.Max + 1);

                int cursorOffset = rng.Next(0, Math.Max(1, totalDays));
                var cursor = start.AddDays(cursorOffset);

                for (int b = 0; b < blocks; b++)
                {
                    if (cursor > end) break;

                    int len = rng.Next(cfg.BlockNights.Min, cfg.BlockNights.Max + 1);

                    for (int k = 0; k < len; k++)
                    {
                        var day = cursor.AddDays(k);
                        if (day > end) break;
                        slots.Add(day);
                    }

                    int gap = rng.Next(cfg.GapDays.Min, cfg.GapDays.Max + 1);
                    cursor = cursor.AddDays(len + gap + rng.Next(1, 4)); 
                }

                return slots;
            }

            for (int i = 0; i < cfg.GenerateCount; i++)
            {
                var id = new PropertyId((1000 + i).ToString(CultureInfo.InvariantCulture));
                var prop = new Property(id, GenName(i));
                var slots = GenerateSlots();

                dict[id] = (prop, slots.ToImmutableHashSet());
            }

            return dict.ToImmutable();
        }
    }
}
