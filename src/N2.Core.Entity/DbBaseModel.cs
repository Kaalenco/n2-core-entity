using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace N2.Core.Entity;

public abstract class DbBaseModel : IDbBaseModel
{
    public DateTime Created { get; set; } = DateTime.UtcNow;

    public Guid CreatedBy { get; set; } = Guid.Empty;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public bool IsRemoved { get; set; }
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    public Guid ModifiedBy { get; set; } = Guid.Empty;
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public DateTime? Removed { get; set; }
    public Guid RemovedBy { get; set; } = Guid.Empty;

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public string SearchField { get; } = string.Empty;

    public void SetCreated(Guid userId)
    {
        Created = DateTime.UtcNow;
        Modified = DateTime.UtcNow;
        if (userId != Guid.Empty)
        {
            CreatedBy = userId;
            ModifiedBy = userId;
        }
    }

    public void SetModified(Guid userId)
    {
        Modified = DateTime.UtcNow;
        if (userId != Guid.Empty)
        {
            ModifiedBy = userId;
        }
    }

    /// <summary>
    ///  Build the model for the given type
    /// </summary>
    protected static ModelBuilder BuildModel<T>([NotNull] ModelBuilder mb, string[] searchFieldColumns) where T : DbBaseModel
    {
        var entityName = typeof(T).Name;
        mb.Entity<T>()
            .HasIndex(b => new { b.PublicId })
            .HasDatabaseName($"UNQ_{entityName}_PublicId")
            .IsUnique();
        mb.Entity<T>()
            .HasIndex(b => new { b.Created })
            .HasDatabaseName($"IX_{entityName}_Created");
        mb.Entity<T>()
            .HasIndex(b => new { b.Modified })
            .HasDatabaseName($"IX_{entityName}_Modified");

        if (searchFieldColumns == null || searchFieldColumns.Length == 0)
        {
            mb.Entity<T>()
                .Property(p => p.SearchField)
                .HasComputedColumnSql("'[' + CreatedBy + ']'");
        }
        else
        {
            var searchText = string.Join("] + ' ' + [", searchFieldColumns);
            mb.Entity<T>()
                .Property(p => p.SearchField)
                .HasComputedColumnSql($"[{searchText}]");
        }

        return mb;
    }

    public abstract T MapTo<T>() where T : class, IBasicListModel, new();
}