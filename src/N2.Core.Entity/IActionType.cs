namespace N2.Core.Entity;

[Flags]
public enum IActionType
{
    None = 0,
    Create = 1,
    Read = 2,
    Update = 4,
    Delete = 8,
    Design = 16,
    AssignRights = 32,
}