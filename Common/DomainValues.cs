namespace EAPlaymateGroup.Common;

public static class DomainValues
{
    public static readonly string[] SystemRoles = ["admin", "staff", "viewer"];
    public static readonly string[] OrderStatuses = ["draft", "completed", "cancelled", "disputed"];
    public static readonly string[] CustomerPaymentStatuses = ["unpaid", "partial", "paid", "refunded"];
    public static readonly string[] OrderMemberRoles = ["player", "leader", "trainer", "bonus"];
    public static readonly string[] PaymentStatuses = ["pending", "paid", "cancelled"];

    public static bool IsSystemRole(string value) => Contains(SystemRoles, value);
    public static bool IsOrderStatus(string value) => Contains(OrderStatuses, value);
    public static bool IsCustomerPaymentStatus(string value) => Contains(CustomerPaymentStatuses, value);
    public static bool IsOrderMemberRole(string value) => Contains(OrderMemberRoles, value);
    public static bool IsPaymentStatus(string value) => Contains(PaymentStatuses, value);

    private static bool Contains(string[] values, string value)
    {
        return values.Contains(value, StringComparer.Ordinal);
    }
}
