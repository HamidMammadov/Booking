using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booking.Application.Common.Pagination
{
    public static class Paging
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        public static (int page, int size) Normalize(int? page, int? size)
        {
            var p = page is > 0 ? page.Value : DefaultPage;
            var s = size is > 0 ? size.Value : DefaultPageSize;
            if (s > MaxPageSize) s = MaxPageSize;
            return (p, s);
        }
    }
}
