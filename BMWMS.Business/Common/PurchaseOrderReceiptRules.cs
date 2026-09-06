using BMWMS.Repository.Models;

namespace BMWMS.Business.Common;

/// <summary>
/// Centralises the quantity semantics shared by PO progress and inbound planning.
/// One physical delivery is one inbound order. Only completed GOOD receipt rows
/// count as accepted PO quantity; active inbound orders reserve their full plan;
/// quantities rejected at the receiving dock never count as accepted inventory
/// and therefore remain eligible for a later physical delivery.
/// </summary>
public static class PurchaseOrderReceiptRules
{
    public static bool IsActiveInbound(string? status)
    {
        return Normalize(status) is "DRAFT" or "ASSIGNED" or "IN_PROGRESS";
    }

    public static bool IsCompletedReceipt(string? status)
    {
        return Normalize(status) is "COMPLETED" or "PUTAWAY_COMPLETED";
    }

    public static PurchaseOrderLineReceiptSnapshot CalculateLine(
        decimal orderedQuantity,
        IEnumerable<InboundOrderItem> inboundItems)
    {
        var items = inboundItems
            .Where(item => Normalize(item.InboundOrder.Status) != "CANCELLED")
            .ToList();

        var activePlannedQuantity = items
            .Where(item => IsActiveInbound(item.InboundOrder.Status))
            .Sum(item => item.ExpectedQuantity);

        var completedDetails = items
            .Where(item => IsCompletedReceipt(item.InboundOrder.Status))
            .SelectMany(item => item.InboundOrderDetails)
            .ToList();

        var acceptedQuantity = completedDetails
            .Where(detail => Normalize(detail.ConditionStatus) == "GOOD")
            .Sum(detail => detail.ReceivedQuantity);
        var legacyRejectedDetailQuantity = completedDetails
            .Where(detail => Normalize(detail.ConditionStatus) is "DAMAGED" or "REJECTED" or "QUARANTINED")
            .Sum(detail => detail.ReceivedQuantity);

        // New receipts store rejected-at-dock quantity on the inbound item rather
        // than creating a stock/location row. Include it in reporting while keeping
        // old DAMAGED/QUARANTINED detail rows readable.
        var rejectedItemQuantity = items
            .Where(item => IsCompletedReceipt(item.InboundOrder.Status))
            .Sum(item => item.DamagedQuantity);
        var rejectedQuantity = Math.Max(legacyRejectedDetailQuantity, rejectedItemQuantity);

        return new PurchaseOrderLineReceiptSnapshot
        {
            OrderedQuantity = orderedQuantity,
            ActivePlannedQuantity = activePlannedQuantity,
            AcceptedQuantity = acceptedQuantity,
            RejectedQuantity = rejectedQuantity,
            AvailableToPlanQuantity = Math.Max(
                0,
                orderedQuantity - activePlannedQuantity - acceptedQuantity)
        };
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }
}

public sealed class PurchaseOrderLineReceiptSnapshot
{
    public decimal OrderedQuantity { get; init; }
    public decimal ActivePlannedQuantity { get; init; }
    public decimal AcceptedQuantity { get; init; }
    public decimal RejectedQuantity { get; init; }
    public decimal AvailableToPlanQuantity { get; init; }
}
