namespace EAPlaymateGroup.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public RequirePermissionAttribute(params string[] permissionCodes)
    {
        PermissionCodes = permissionCodes;
        PermissionCode = permissionCodes.FirstOrDefault() ?? string.Empty;
    }

    public string PermissionCode { get; }
    public IReadOnlyList<string> PermissionCodes { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PublicApiAttribute : Attribute
{
}
