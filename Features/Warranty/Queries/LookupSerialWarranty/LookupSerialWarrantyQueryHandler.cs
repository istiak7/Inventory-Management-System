using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Warranty.Shared;
using Inventory_Management_System.Features.Warranty.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Queries.LookupSerialWarranty
{
    // The eligibility screen. Everything it reports is derived — nothing about cover is stored
    // until a claim is actually opened — so scanning a serial can never mutate anything, and the
    // same rules run again server-side at intake (CreateWarrantyClaimHandler) rather than trusting
    // whatever the counter was shown a minute ago.
    public class LookupSerialWarrantyQueryHandler(
        AppDbContext _dbContext,
        ILogger<LookupSerialWarrantyQueryHandler> _logger
    ) : IRequestHandler<LookupSerialWarrantyQuery, Result>
    {
        public async Task<Result> Handle(LookupSerialWarrantyQuery request, CancellationToken cancellationToken)
        {
            var serialNumber = request.SerialNumber?.Trim();
            if (string.IsNullOrEmpty(serialNumber))
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "A serial number is required." };

            try
            {
                var serial = await _dbContext.ProductSerials
                    .AsNoTracking()
                    .Where(s => s.SerialNumber == serialNumber)
                    .Select(s => new
                    {
                        s.Id,
                        s.SerialNumber,
                        s.Status,
                        s.SoldDate,
                        s.WarrantyMonths,
                        s.BranchId,
                        BranchName = s.Branch.Name,
                        s.ProductVariantId,
                        Sku = s.ProductVariant.SKU,
                        ProductName = s.ProductVariant.Product.ProductName
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (serial == null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = $"No unit with serial number '{serialNumber}' exists." };

                // Claims already logged against this unit — shown alongside the card so the counter
                // sees a repeat visitor before opening a duplicate job.
                var claims = (await _dbContext.WarrantyClaims
                        .AsNoTracking()
                        .Where(c => c.ProductSerialId == serial.Id)
                        .OrderByDescending(c => c.Id)
                        .ProjectToRow()
                        .ToListAsync(cancellationToken))
                    .Select(row => row.ToResponse())
                    .ToList();

                var liveClaim = claims.FirstOrDefault(c =>
                    c.Status == nameof(WarrantyClaimStatus.Open) || c.Status == nameof(WarrantyClaimStatus.InRepair));

                var saleDetailsId = await WarrantyTerms.ResolveSaleDetailsIdAsync(_dbContext, serial.Id, cancellationToken);

                // Never sold and never issued as a replacement: it is shop stock, not a customer's
                // unit. Report it plainly rather than dressing it up as an expired warranty.
                if (saleDetailsId == null)
                {
                    var unsold = new WarrantySerialLookupResponse(
                        serial.Id, serial.SerialNumber, serial.Status.ToString(),
                        serial.ProductVariantId, serial.Sku, serial.ProductName,
                        serial.BranchId, serial.BranchName,
                        null, serial.WarrantyMonths, null, false, 0,
                        false, "This unit has not been sold, so there is no warranty to claim against.",
                        null, claims);

                    return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Serial found", Data = unsold };
                }

                var line = await _dbContext.SaleDetails
                    .AsNoTracking()
                    .Where(d => d.Id == saleDetailsId)
                    .Select(d => new
                    {
                        d.Id,
                        d.SaleId,
                        d.WarrantyMonths,
                        d.ProductSerialId,
                        OriginalSerialNumber = d.ProductSerial != null ? d.ProductSerial.SerialNumber : null,
                        d.CustomerSale.InvoiceNumber,
                        d.CustomerSale.SaleDate,
                        d.CustomerSale.CustomerId,
                        CustomerName = d.CustomerSale.Customer.Name,
                        CustomerPhoneNumber = d.CustomerSale.Customer.PhoneNumber
                    })
                    .FirstAsync(cancellationToken);

                // The line's own months are the terms the customer was sold; the serial's are the
                // supplier-side figure it arrived with, used only if the line never recorded any.
                var warrantyMonths = line.WarrantyMonths ?? serial.WarrantyMonths;
                var expiry = WarrantyTerms.ExpiryFor(serial.SoldDate, warrantyMonths);
                var now = DateTime.UtcNow;
                var underWarranty = WarrantyTerms.IsUnderWarranty(expiry, now);

                // The line points at a different unit => this one reached the customer as a swap,
                // and the invoice above belongs to the unit it replaced.
                var isReplacementUnit = line.ProductSerialId != serial.Id;

                var sale = new WarrantySaleReference(
                    line.SaleId, line.InvoiceNumber, line.SaleDate, line.Id,
                    line.CustomerId, line.CustomerName, line.CustomerPhoneNumber,
                    isReplacementUnit, isReplacementUnit ? line.OriginalSerialNumber : null);

                var (canOpenClaim, message) = Eligibility(serial.Status, expiry, underWarranty, liveClaim?.ClaimNumber, now);

                var response = new WarrantySerialLookupResponse(
                    serial.Id, serial.SerialNumber, serial.Status.ToString(),
                    serial.ProductVariantId, serial.Sku, serial.ProductName,
                    serial.BranchId, serial.BranchName,
                    serial.SoldDate, warrantyMonths, expiry, underWarranty,
                    WarrantyTerms.DaysRemaining(expiry, now),
                    canOpenClaim, message, sale, claims);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Serial found", Data = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up warranty for serial {SerialNumber}", serialNumber);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while looking up the warranty." };
            }
        }

        /// <summary>
        /// The single verdict, in the order a counter would reach it: is the unit still the
        /// customer's, is it covered, and is it already on a bench somewhere. Each refusal names
        /// its own reason — "not eligible" alone sends staff hunting for a fault that isn't theirs.
        /// </summary>
        private static (bool CanOpenClaim, string Message) Eligibility(
            SerialStatus status, DateTime? expiry, bool underWarranty, string? liveClaimNumber, DateTime now)
        {
            if (status == SerialStatus.RmaReturned)
                return (false, "This unit was taken back by the shop under an earlier claim and replaced — the customer holds the replacement now.");

            if (status == SerialStatus.Defective)
                return (false, "This unit is marked defective and is no longer with the customer.");

            if (status != SerialStatus.Sold)
                return (false, "This unit is back in shop stock, so there is no active warranty to claim against.");

            if (expiry == null)
                return (false, "No warranty was sold with this unit.");

            if (!underWarranty)
                return (false, $"Warranty expired on {expiry:dd MMM yyyy}. This unit is no longer covered.");

            if (liveClaimNumber != null)
                return (false, $"Claim {liveClaimNumber} is already open for this unit.");

            var days = WarrantyTerms.DaysRemaining(expiry, now);
            return (true, $"Under warranty — {days} day(s) remaining, expires {expiry:dd MMM yyyy}.");
        }
    }
}
