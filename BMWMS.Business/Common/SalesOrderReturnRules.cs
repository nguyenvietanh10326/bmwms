using BMWMS.Repository.Models;

namespace BMWMS.Business.Common;

/// <summary>
/// Centralises the quantity semantics for customer returns linked to an SO.
/// A fulfilled SO quantity is only a return ceiling. Active inbound orders
/// reserve their plan; completed receipts consume that ceiling only for GOOD
/// quantity. Short and rejected-at-dock quantity can therefore be planned again.
/// </summary>
public static class SalesOrderReturnRules
{
    public static SalesOrderReturnLineSnapshot CalculateLine(
        decimal fulfilledQuantity,
        IEnumerable<InboundOrderItem> inboundItems)
    {
        var items = inboundItems
            .Where(item => Normalize(item.InboundOrder.SourceType) == "SALES_RETURN" &&
                           Normalize(item.InboundOrder.Status) != "CANCELLED")
            .ToList();

        var activePlannedQuantity = items
            .Where(item => PurchaseOrderReceiptRules.IsActiveInbound(item.InboundOrder.Status))
            .Sum(item => item.ExpectedQuantity);

        var completedItems = items
            .Where(item => PurchaseOrderReceiptRules.IsCompletedReceipt(item.InboundOrder.Status))
            .ToList();
        var completedDetails = completedItems
            .SelectMany(item => item.InboundOrderDetails)
            .ToList();

        var acceptedQuantity = completedDetails
            .Where(detail => Normalize(detail.ConditionStatus) == "GOOD")
            .Sum(detail => detail.ReceivedQuantity);
        var legacyRejectedDetailQuantity = completedDetails
            .Where(detail => Normalize(detail.ConditionStatus) is "DAMAGED" or "REJECTED" or "QUARANTINED")
            .Sum(detail => detail.ReceivedQuantity);
        var rejectedItemQuantity = completedItems.Sum(item => item.DamagedQuantity);

        return new SalesOrderReturnLineSnapshot
        {
            FulfilledQuantity = fulfilledQuantity,
            ActivePlannedQuantity = activePlannedQuantity,
            AcceptedQuantity = acceptedQuantity,
            RejectedQuantity = Math.Max(legacyRejectedDetailQuantity, rejectedItemQuantity),
            AvailableToPlanQuantity = Math.Max(
                0,
                fulfilledQuantity - activePlannedQuantity - acceptedQuantity)
        };
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }
}

public sealed class SalesOrderReturnLineSnapshot
{
    public decimal FulfilledQuantity { get; init; }
    public decimal ActivePlannedQuantity { get; init; }
    public decimal AcceptedQuantity { get; init; }
    public decimal RejectedQuantity { get; init; }
    public decimal AvailableToPlanQuantity { get; init; }
}
