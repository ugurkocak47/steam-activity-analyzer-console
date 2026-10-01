using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamGameAnalyzerConsole.Helpers
{
    public class PaginationHelper<T>
    {
        private readonly IEnumerable<T> _items;

        public int PageSize { get; }
        public int CurrentPage { get; set; }

        // Evaluates the count once without throwing exceptions on empty lists
        public int TotalPages => (int)Math.Ceiling((double)_items.Count() / PageSize);

        // Standard constructor allows us to validate inputs easily
        public PaginationHelper(IEnumerable<T> items, int pageSize, int currentPage = 1)
        {
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
            if (currentPage <= 0) throw new ArgumentOutOfRangeException(nameof(currentPage), "Current page must be greater than zero.");

            _items = items ?? throw new ArgumentNullException(nameof(items));
            PageSize = pageSize;
            CurrentPage = currentPage;
        }

        public IEnumerable<T> Paginate()
        {
            // Skip() and Take() handle bounds checking automatically.
            // Using CurrentPage (the property), NOT a stagnant parameter.
            return _items.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
        }

        public IEnumerable<T> NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                return Paginate();
            }

            // Actually returns an empty list as your original comment intended
            return Enumerable.Empty<T>();
        }

        public IEnumerable<T> PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                return Paginate();
            }

            return Enumerable.Empty<T>();
        }
    }
}