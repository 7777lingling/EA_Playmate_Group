namespace EAPlaymateGroup.Common;

public static class PermissionCodes
{
    public static readonly string[] All =
    [
        "Member.View",
        "Member.Create",
        "Member.Edit",
        "Member.Delete",
        "Gift.View",
        "Gift.Create",
        "Gift.Edit",
        "Gift.Delete",
        "Order.View",
        "Order.Create",
        "Order.Edit",
        "Order.Cancel",
        "Settlement.View",
        "Settlement.Close",
        "Settlement.Export",
        "Account.Manage",
        "Organization.Manage",
        "Audit.View"
    ];

    public static bool IsValid(string value) =>
        All.Contains(value, StringComparer.Ordinal);
}
