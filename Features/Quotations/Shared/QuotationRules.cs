using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Quotations.Shared.Dtos;
using Inventory_Management_System.Shared;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Quotations.Shared
{
    // The quotation business rules in one place: expiry, which status can follow which,
    // when a quotation may still be edited or deleted, and how its lines and totals are built.
    public static class QuotationRules
    {
        public const string DefaultUom = "Pcs";

        // Date-only values are kept at 00:00 UTC of that day (like other date-only inputs).
        public static DateTime ToStoredDate(DateTime value) => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

        // Today in the shop's calendar, in the same stored form.
        public static DateTime TodayStored => DateTime.SpecifyKind(BusinessClock.Today, DateTimeKind.Utc);

        // Draft and Sent are open offers; past ValidUntil they are Expired.
        public static QuotationStatus EffectiveStatus(QuotationStatus status, DateTime validUntil) =>
            (status is QuotationStatus.Draft or QuotationStatus.Sent) && validUntil < TodayStored
                ? QuotationStatus.Expired
                : status;

        public static bool CanEdit(QuotationStatus effective) =>
            effective is QuotationStatus.Draft or QuotationStatus.Sent
                or QuotationStatus.Expired or QuotationStatus.Rejected;

        // Hard delete is fine while nothing was agreed: nothing else points at the quotation.
        public static bool CanDelete(QuotationStatus effective) => CanEdit(effective);

        // Allowed manual status changes. Converted is never set by hand.
        public static bool CanChange(QuotationStatus from, QuotationStatus to) => (from, to) switch
        {
            (QuotationStatus.Draft, QuotationStatus.Sent or QuotationStatus.Accepted
                or QuotationStatus.Rejected or QuotationStatus.Expired) => true,
            (QuotationStatus.Sent, QuotationStatus.Accepted or QuotationStatus.Rejected
                or QuotationStatus.Expired) => true,
            // The customer backed out after agreeing.
            (QuotationStatus.Accepted, QuotationStatus.Rejected) => true,
            (QuotationStatus.Expired, QuotationStatus.Rejected) => true,
            // Re-open a rejected offer (it shows Expired again if its date has passed).
            (QuotationStatus.Rejected, QuotationStatus.Draft) => true,
            _ => false,
        };

        // Filter by the status the user sees, so "Expired" also finds open offers past their date.
        public static IQueryable<Quotation> WhereEffectiveStatus(this IQueryable<Quotation> query, QuotationStatus status)
        {
            var today = TodayStored;
            return status switch
            {
                QuotationStatus.Expired => query.Where(q =>
                    q.Status == QuotationStatus.Expired ||
                    ((q.Status == QuotationStatus.Draft || q.Status == QuotationStatus.Sent) && q.ValidUntil < today)),
                QuotationStatus.Draft or QuotationStatus.Sent => query.Where(q => q.Status == status && q.ValidUntil >= today),
                _ => query.Where(q => q.Status == status),
            };
        }

        public static Result Fail(int statusCode, string message) => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Status = "Error",
            Message = message
        };

        // Builds the snapshot lines and totals for a quotation from the request.
        // Returns an error Result when a product is missing or the money does not add up.
        public static async Task<Result?> ApplyLinesAsync(
            AppDbContext db,
            Quotation quotation,
            IReadOnlyList<QuotationItemRequest> items,
            decimal discountAmount,
            decimal taxAmount,
            CancellationToken cancellationToken)
        {
            var variantIds = items.Select(i => i.ProductVariantId).Distinct().ToList();
            var variants = await db.ProductVariants
                .Include(v => v.Product)
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, cancellationToken);

            var lines = new List<QuotationDetails>();
            decimal subTotal = 0;

            foreach (var item in items)
            {
                if (!variants.TryGetValue(item.ProductVariantId, out var variant))
                    return Fail(404, $"Product variant with id {item.ProductVariantId} not found.");

                var unitPrice = decimal.Round(item.UnitPrice ?? variant.SellingPrice, 2, MidpointRounding.AwayFromZero);
                if (unitPrice <= 0)
                    return Fail(400, $"Enter a unit price above 0 for '{variant.Product.ProductName}'.");

                var gross = unitPrice * item.Quantity;
                if (item.DiscountAmount > gross)
                    return Fail(400, $"The discount on '{variant.Product.ProductName}' ({item.DiscountAmount}) is more than the line amount ({gross}).");

                var lineTotal = gross - item.DiscountAmount;
                subTotal += lineTotal;

                lines.Add(new QuotationDetails
                {
                    ProductVariantId = variant.Id,
                    ProductName = variant.Product.ProductName,
                    Sku = variant.SKU,
                    Uom = string.IsNullOrWhiteSpace(item.Uom) ? DefaultUom : item.Uom.Trim(),
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TotalAmount = lineTotal,
                    Quotation = quotation,
                    ProductVariant = variant,
                });
            }

            if (discountAmount > subTotal)
                return Fail(400, $"The overall discount ({discountAmount}) is more than the subtotal ({subTotal}).");

            quotation.QuotationDetails.Clear();
            foreach (var line in lines)
                quotation.QuotationDetails.Add(line);

            quotation.SubTotal = subTotal;
            quotation.DiscountAmount = discountAmount;
            quotation.TaxAmount = taxAmount;
            quotation.TotalAmount = subTotal - discountAmount + taxAmount;
            return null;
        }

        public static string? CleanText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
