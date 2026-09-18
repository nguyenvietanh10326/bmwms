namespace BMWMS.Business.Common;

public static class OrderWorkflowNotes
{
    // Decision reasons are always stored in the same-transaction audit event.
    // A convenience note must neither truncate existing user data nor exceed
    // the SQL Notes nvarchar(2000) limit and block the business transition.
    public static string? AppendIfFits(string? existing, string decision, int maxLength = 2000)
    {
        var combined = string.IsNullOrWhiteSpace(existing) ? decision : $"{existing}\n{decision}";
        return combined.Length <= maxLength ? combined : existing;
    }
}
