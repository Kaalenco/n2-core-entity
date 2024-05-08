using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace N2.Core.Entity;

public static class DbQueryExtensions
{
    public const int DefaultPageSize = 15;
    public const int MaxPageSize = 100;

    public static ReadOnlyCollection<T> AsReadOnlyListItems<TData, T>(
        this IEnumerable<TData> dbRecords,
        [NotNull] Func<TData, T> mapToModel
        )
        where TData : class, IDbBaseModel, new()
        where T : class, IBasicListModel, new()
    {
        var result = new List<T>();
        if (dbRecords == null)
        {
            return new ReadOnlyCollection<T>(result);
        }

        foreach (var dbRecord in dbRecords)
        {
            if (dbRecord == null)
            {
                continue;
            }

            var model = mapToModel.Invoke(dbRecord);
            result.Add(model);
        }
        return new ReadOnlyCollection<T>(result);
    }

    public static async Task<(IEnumerable<T> records, int count)> SearchAsync<T>(
            this IQueryable<T> query,
            string? search,
            int page,
            int pageSize,
            string sortColumn,
            bool sortDesc
        )
        where T : IDbBaseModel
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = DefaultPageSize;
        }

        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        var searchQuery = query;

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (Guid.TryParse(search, out var guid))
            {
                searchQuery = searchQuery
                    .Where(x => x.PublicId == guid);
            }
            else if (search.Contains(' ', StringComparison.Ordinal))
            {
                foreach (var term in search.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    searchQuery = searchQuery
                        .Where(x => x.SearchField.Contains(term));
                }
            }
            else
            {
                searchQuery = searchQuery
                    .Where(x => x.SearchField.Contains(search));
            }
        }

        var count = await searchQuery.CountAsync();

        if (!string.IsNullOrEmpty(sortColumn))
        {
            searchQuery = sortDesc
            ? searchQuery.OrderByColumnDescending(sortColumn)
            : searchQuery.OrderByColumn(sortColumn);
        }
        else
        {
            searchQuery = searchQuery.OrderBy(x => x.Id);
        }

        var skipCount = (page - 1) * pageSize;
        if (skipCount > count)
        {
            skipCount -= pageSize;
        }

        var records = await searchQuery
            .Skip(skipCount)
            .Take(pageSize)
            .ToListAsync();
        return (records, count);
    }
}