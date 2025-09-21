using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booking.Infrastructure.Options
{
    public sealed class DataSeedOptions
    {
        public int GenerateCount { get; init; } = 2000;
        public int? RandomSeed { get; init; } = null;

        public DateOnly SeedStart { get; init; } = new(2025, 07, 01);
        public DateOnly SeedEnd { get; init; } = new(2025, 09, 30);

        public (int Min, int Max) BlocksPerProperty { get; init; } = (1, 4);
        public (int Min, int Max) BlockNights { get; init; } = (1, 5);

        public (int Min, int Max) GapDays { get; init; } = (0, 2);

        public string[] Locales { get; init; } =
        [
            "Bakı", "Gəncə", "Sumqayıt", "Şəki", "Qəbələ",
            "Quba", "Lənkəran", "Naxçıvan", "Mingəçevir", "Şuşa"
        ];

        public string[] NameAdjectives { get; init; } =
        [
            "Sahil", "Dağlıq", "Sakit", "Gözəl", "Yaşıl",
            "Tarixi", "Müasir", "Ənənəvi", "Panoramlı", "Mərmərli",
            "Qonaqlı", "Minimalist", "Baxımlı", "Təpəlik"
        ];

        public string[] NameShapes { get; init; } =
        [
            "Mənzil", "Villa", "Bağ Evi", "Dağ Evi", "Kottec",
            "Həyət Evi", "Qəsr", "Bina", "Loft", "Otaq"
        ];
    }
}