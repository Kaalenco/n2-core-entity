using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace N2.Core.Entity;

public abstract class CoreDataContext(DbContextOptions options) : DbContext(options), ICoreDataContext
{
    public virtual DbSet<ChangeLog> ChangeLog { get; set; }
    public IQueryable<IChangeLog> ChangeLogs => ChangeLog.AsNoTracking();
    public string CurrentDatabaseName => Database.GetDbConnection().Database;

    public void AddChangeLog<T>(
    Guid publicId,
    string message,
    Guid userId,
    string userName)
    where T : class
    {
        var logEntry = new ChangeLog
        {
            TableName = typeof(T).Name,
            ReferenceId = publicId,
            Message = message,
            CreatedBy = userId,
            CreatedByName = userName,
            Created = DateTime.UtcNow
        };
        ChangeLog.Add(logEntry);
    }

    public void AddChangeLog(IChangeLog changeLog)
    {
        if (changeLog == null)
        {
            return;
        }

        var logEntry = new ChangeLog
        {
            TableName = changeLog.TableName,
            ReferenceId = changeLog.ReferenceId,
            Message = changeLog.Message,
            CreatedBy = changeLog.CreatedBy,
            CreatedByName = changeLog.CreatedByName,
            Created = changeLog.Created
        };
        ChangeLog.Add(logEntry);
    }

    public void AddRecord<T>(T model) where T : class
    {
        if (model == null)
        {
            return;
        }

        var dbModel = model as IDbBaseModel;
        if (dbModel != null)
        {
            if (dbModel.PublicId == Guid.Empty)
            {
                dbModel.PublicId = Guid.NewGuid();
            }
            dbModel.Created = DateTime.UtcNow;
            dbModel.Modified = DateTime.UtcNow;
        }

        Set<T>().Add(model);
    }

    public async Task<(int resultCode, string message)> DeleteAsync<T>(Guid publicId) where T : class
    {
        var dbSet = Set<T>();
        if (typeof(IDbBaseModel).IsAssignableFrom(typeof(T)))
        {
            var baseItem = await dbSet.FirstOrDefaultAsync(x => ((IDbBaseModel)x).PublicId == publicId);
            var baseModel = baseItem as IDbBaseModel;
            if (baseModel == null)
            {
                return (404, "Not found");
            }
            baseModel.Removed = DateTime.UtcNow;
            baseModel.IsRemoved = true;
            return new(204, "Removed");
        }

        var dbItem = await dbSet.FindAsync(publicId);

        if (dbItem == null)
        {
            return (404, "Not found");
        }
        dbSet.Remove(dbItem);
        return new(204, "Removed");
    }

    public Task<T?> FindRecordAsync<T>(Guid publicId) where T : class
    {
        if (typeof(IDbBaseModel).IsAssignableFrom(typeof(T)))
        {
            return Set<T>().FirstOrDefaultAsync(x => ((IDbBaseModel)x).PublicId == publicId);
        }
        return Set<T>().FindAsync(publicId).AsTask();
    }

    public abstract Task<List<KeyValuePair<string, string>>> GetSelectListAsync(string tableName);
    public async Task<(int code, string message)> SaveChangesAsync()
    {
        // Check if ChangeLog needs to be updated
        try
        {
            var modified = await base.SaveChangesAsync();
            return new(200, $"{modified} records modified");
        }
        catch (DbException ex)
        {
            return new(500, ex.Message);
        }
    }

    protected override void OnModelCreating([NotNull] ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add model configurations for generic tables
        Entity.ChangeLog.BuildModel(modelBuilder);
    }
}