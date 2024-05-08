using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using N2.Core.Identity;

namespace N2.Core.Entity;

public abstract class CoreDesignComponent<TContext, TData, TView, TDetail> : IDesignComponent<TView, TDetail>
    where TContext : class, ICoreDataContext
    where TData : class, IDbBaseModel, new()
    where TView : class, IBasicListModel, new()
    where TDetail : class, IBasicListModel, new()
{
    // The data context is the unit of work that is updated at the end of an operation.
    protected ICoreDataContextFactory<TContext> DbFactory { get; }

    // The RecordSet is an iqueryable and
    // could include related data for overviews.
    protected Func<TContext, IQueryable<TData>> RecordSet { get; }

    // The initialize view function is used to create a new view model.
    protected Func<TDetail> InitializeView { get; }

    // The map record to view function is used to map a record to a list view model.
    protected Func<TData, TView> MapRecordToList { get; }

    // The map record to view function is used to map a record to a view model.
    protected Func<TData, Task<TDetail>> MapRecordToView { get; }

    // The map view to record function is used to map data from the view model on an existing.
    protected Func<TDetail, TData, Task<TData>> MapViewToRecord { get; }

    // The verify access function is optional and can be used to
    // check if the user has access to the data.
    protected Func<IUserContext, IActionType, Guid, bool>? VerifyAccess { get; }

    public ReadOnlyCollection<TView>? Items { get; private set; }

    protected CoreDesignComponent(
        [NotNull] ICoreDataContextFactory<TContext> dataContextFactory,
        [NotNull] Func<TContext, IQueryable<TData>> recordListView,
        [NotNull] Func<TDetail> initializeView,
        [NotNull] Func<TData, TView> mapRecordToList,
        [NotNull] Func<TData, Task<TDetail>> mapRecordToView,
        [NotNull] Func<TDetail, TData, Task<TData>> mapViewToRecord,
        Func<IUserContext, IActionType, Guid, bool>? verifyAccess = null
        )
    {
        DbFactory = dataContextFactory;
        RecordSet = recordListView;
        InitializeView = initializeView;
        MapRecordToList = mapRecordToList;
        MapRecordToView = mapRecordToView;
        MapViewToRecord = mapViewToRecord;
        VerifyAccess = verifyAccess;
    }

    public async Task<RequestResult> SaveItemAsync([NotNull] TDetail model, IUserContext? userContext)
    {
        if (userContext == null)
        {
            return new RequestResult(403, "Unauthorized");
        }
        if (!userContext.CanDesign())
        {
            return new RequestResult(403, "No design rights");
        }

#pragma warning disable CA1031 // Do not catch general exception types
        using var Db = await DbFactory.CreateAsync();
        var recordSet = RecordSet.Invoke(Db);
        try
        {
            TData? record;
            if (model.DbItem)
            {
                record = await recordSet.FirstOrDefaultAsync(x => x.PublicId == model.PublicId);
                if (record == null)
                {
                    return new RequestResult(404, $"Could not find {model.PublicId}");
                }

                record.UpdateTracking(userContext.UserId);
                record = await MapViewToRecord.Invoke(model, record);

                Db.AddChangeLog<TData>(record.PublicId, "Modified", userContext.UserId, userContext.UserName);
            }
            else if (CanDesign(userContext, model.PublicId))
            {
                record = new TData();
                if (model.PublicId != Guid.Empty)
                {
                    record.PublicId = model.PublicId;
                }
                record.TrackingSetCreated(userContext.UserId);
                record = await MapViewToRecord.Invoke(model, record);
                Db.AddRecord(record);
                Db.AddChangeLog<TData>(record.PublicId, "Added", userContext.UserId, userContext.UserName);
            }
            else
            {
                return new RequestResult(403, "No design rights");
            }

            var (result, message) = await Db.SaveChangesAsync();
            return new RequestResult(result, message);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException != null)
            {
                return new RequestResult(406, ex.InnerException.Message);
            }

            return new RequestResult(500, ex.Message);
        }
        catch (Exception ex)
        {
            return new RequestResult(500, ex.Message);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private bool CanDesign(IUserContext userContext, Guid publicId)
    {
        if (userContext == null)
        {
            return false;
        }

        return (VerifyAccess == null)
            ? userContext.CanDesign()
            : VerifyAccess.Invoke(userContext, IActionType.Design, publicId);
    }

    public async Task<RequestResult> RemoveItemAsync(Guid publicId, IUserContext? userContext)
    {
        if (userContext == null)
        {
            return  new RequestResult(403, "Unauthorized");
        }
        if (!CanDesign(userContext, publicId))
        {
            return new RequestResult(403, "No design rights");
        }

        using var Db = await DbFactory.CreateAsync();
        var (resultcode, message) = await Db.DeleteAsync<TData>(publicId);
        if (resultcode.IsErrorCode())
        {
            return new RequestResult(resultcode, message);
        }

        Db.AddChangeLog<TData>(publicId, "Removed", userContext.UserId, userContext.UserName);
        (resultcode, message) = await Db.SaveChangesAsync();
        return new RequestResult(resultcode, message);
    }

    public async Task<TDetail?> InitializeModelAsync(string? id, IUserContext? userContext)
    {
        if (userContext == null)
        {
            return default;
        }
        if (!userContext.IsAuthenticated)
        {
            return default;
        }

        TDetail? result;
        if (Guid.TryParse(id, out var publicId))
        {
            result = await ReadFromDatabaseAsync(publicId, userContext);
            if (result != null)
            {
                result.DbItem = true;
                return result;
            }
        }
        if (publicId == Guid.Empty)
        {
            publicId = Guid.NewGuid();
        }

        result = InitializeView.Invoke();
        result.InitTracking(publicId, userContext.UserId);
        return result;
    }

    public async Task<bool> LoadItemsAsync([NotNull] PagingInfo pagingInfo, IUserContext? userContext)
    {
        if (userContext == null)
        {
            return false;
        }
        if (!userContext.IsAuthenticated)
        {
            return false;
        }
        using var Db = await DbFactory.CreateAsync();
        var recordSet = RecordSet
            .Invoke(Db)
            .Where(m => !m.IsRemoved);
        var (items, count) = await recordSet
            .SearchAsync(
                pagingInfo.Q,
                pagingInfo.Page,
                pagingInfo.PageSize,
                pagingInfo.Sort,
                pagingInfo.SortDesc
        );

        Items = items.AsReadOnlyListItems(MapRecordToList);
        var totalPages = (int)Math.Ceiling(count / (double)pagingInfo.PageSize);
        var currentPage = pagingInfo.Page;
        if (currentPage > totalPages)
        {
            currentPage = totalPages;
        }

        if (currentPage < 1)
        {
            currentPage = 1;
        }

        pagingInfo.Page = currentPage;
        pagingInfo.TotalItems = count;
        pagingInfo.TotalPages = totalPages;
        return true;
    }

    public async Task<TDetail?> ReadFromDatabaseAsync(Guid publicId, IUserContext? userContext)
    {
        using var Db = await DbFactory.CreateAsync();
        var recordSet = RecordSet.Invoke(Db);

        if (userContext == null)
        {
            return default;
        }
        if (!userContext.IsAuthenticated)
        {
            return default;
        }

        var record = await recordSet.FirstOrDefaultAsync(x => x.PublicId == publicId && !x.IsRemoved);
        if (record == null)
        {
            return default;
        }

        return await MapRecordToView.Invoke(record);
    }
}

internal static class ErrorCodeExtensions
{
    public static bool IsErrorCode(this int value) => value >= 400 && value < 600;
}