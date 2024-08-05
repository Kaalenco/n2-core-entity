namespace N2.Core.Entity;

public static class DbRecordExtensions
{
    public static void UpdateTracking(this IDbBaseModel dbRecord, Guid userId)
    {
        if (dbRecord == null)
        {
            return;
        }

        dbRecord.Modified = DateTime.UtcNow;
        if (userId != Guid.Empty)
        {
            dbRecord.ModifiedBy = userId;
        }
    }

    public static void InitTracking(this IModifiable model, Guid publicId, Guid userId)
    {
        if (model == null)
        {
            return;
        }

        model.DbItem = false;
        model.PublicId = publicId;
        model.Created = DateTime.UtcNow;
        model.Modified = DateTime.UtcNow;
        if (userId != Guid.Empty)
        {
            model.CreatedBy = userId;
            model.ModifiedBy = userId;
        }
    }

    public static void TrackingSetCreated(this IDbBaseModel dbRecord, Guid userId)
    {
        if (dbRecord == null)
        {
            return;
        }

        dbRecord.Created = DateTime.UtcNow;
        dbRecord.Modified = DateTime.UtcNow;
        if (userId != Guid.Empty)
        {
            dbRecord.CreatedBy = userId;
            dbRecord.ModifiedBy = userId;
        }
    }

    public static void ReadTracking(this IModifiable model, IDbBaseModel dbRecord)
    {
        if (model == null)
        {
            return;
        }

        if (dbRecord == null)
        {
            return;
        }

        model.DbItem = true;
        model.PublicId = dbRecord.PublicId;
        model.Created = dbRecord.Created;
        model.Modified = dbRecord.Modified;
        model.CreatedBy = dbRecord.CreatedBy;
        model.ModifiedBy = dbRecord.ModifiedBy;
    }
}