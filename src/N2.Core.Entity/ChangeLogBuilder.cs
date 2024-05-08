using Microsoft.EntityFrameworkCore;

namespace N2.Core.Entity;

public static class ChangeLogBuilder
{
    public static ModelBuilder BuildModel(ModelBuilder modelBuilder)
    {
        ChangeLog.BuildModel(modelBuilder);
        return modelBuilder;
    }
}