using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace N2.Core.Entity;

public class ChangeLog : IChangeLog
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid LogRecordId { get; set; } = Guid.NewGuid();
    public Guid ReferenceId { get; set; } = Guid.Empty;

    [MaxLength(200)]
    public string TableName { get; set; } = string.Empty;

    public DateTime Created { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; } = Guid.Empty;

    [MaxLength(200)]
    public string CreatedByName { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string Message { get; set; } = string.Empty;

    public static ModelBuilder BuildModel([NotNull] ModelBuilder mb)
    {
        mb.Entity<ChangeLog>()
            .HasIndex(b => new { b.LogRecordId })
            .HasDatabaseName("UNQ_ChangeLog_LogRecordId")
            .IsUnique();
        mb.Entity<ChangeLog>()
            .HasIndex(b => new { b.ReferenceId })
            .HasDatabaseName("IX_ChangeLog_ReferenceId");
        mb.Entity<ChangeLog>()
            .HasIndex(b => new { b.CreatedBy })
            .HasDatabaseName("IX_ChangeLog_CreatedBy");
        mb.Entity<ChangeLog>()
            .HasIndex(b => new { b.TableName })
            .HasDatabaseName("IX_ChangeLog_TableName");
        return mb;
    }
}
