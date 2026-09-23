using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Warranty.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventory_Management_System.Features.Warranty.Command.CreateWarrantyClaim
{
    public class CreateWarrantyClaimHandler(
        AppDbContext _dbContext,
        ILogger<CreateWarrantyClaimHandler> _logger
    ) : IRequestHandler<CreateWarrantyClaimCommand, Result>
    {
        public async Task<Result> Handle(
            CreateWarrantyClaimCommand request,
            CancellationToken cancellationToken)
        {
            var serialNumber = request.SerialNumber.Trim();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var serial = await _dbContext.ProductSerials
                    .FirstOrDefaultAsync(
                    s => s.SerialNumber == serialNumber,
                    cancellationToken);

                if (serial == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Error",
                        Message = $"No unit with serial number '{serialNumber}' exists."
                    };

                if (serial.Status != SerialStatus.Sold)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Serial '{serialNumber}' is not with a customer (status {serial.Status}), so it has no warranty to claim."
                    };

                // Lock the unit so a double click cannot open two claims for it.
                await _dbContext.LockRowAsync<ProductSerial>(serial.Id, cancellationToken);

                var saleDetailsId = await WarrantyTerms.ResolveSaleDetailsIdAsync(
                    _dbContext,
                    serial.Id,
                    cancellationToken);

                if (saleDetailsId == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Serial '{serialNumber}' has never been sold, so there is no warranty to claim against."
                    };

                var saleDetail = await _dbContext.SaleDetails
                    .Include(d => d.CustomerSale)
                    .FirstAsync(
                    d => d.Id == saleDetailsId,
                    cancellationToken);

                var warrantyMonths = saleDetail.WarrantyMonths ?? serial.WarrantyMonths;
                var expiry = WarrantyTerms.ExpiryFor(serial.SoldDate, warrantyMonths);

                if (expiry == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"No warranty was sold with serial '{serialNumber}'."
                    };

                if (!WarrantyTerms.IsUnderWarranty(expiry, DateTime.UtcNow))
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Warranty for serial '{serialNumber}' expired on {expiry:dd MMM yyyy}. This unit is no longer covered."
                    };

                var liveClaim = await _dbContext.WarrantyClaims
                    .AsNoTracking()
                    .Where(c => c.ProductSerialId == serial.Id &&
                                (c.Status == WarrantyClaimStatus.Open || c.Status == WarrantyClaimStatus.InRepair))
                    .Select(c => c.ClaimNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                if (liveClaim != null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 409,
                        Status = "Error",
                        Message = $"Claim {liveClaim} is already open for serial '{serialNumber}'."
                    };

                var branchId = request.BranchId ?? serial.BranchId;
                var branch = await _dbContext.Branches.FirstOrDefaultAsync(
                    b => b.Id == branchId,
                    cancellationToken);

                if (branch == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Error",
                        Message = "Branch not found."
                    };

                var customer = await _dbContext.Customers
                    .FirstAsync(c => c.Id == saleDetail.CustomerSale.CustomerId, cancellationToken);

                var now = DateTime.UtcNow;
                var claim = new WarrantyClaim
                {
                    ClaimNumber = await GenerateClaimNumberAsync(now, cancellationToken),
                    ProductSerialId = serial.Id,
                    SaleDetailsId = saleDetail.Id,
                    CustomerId = customer.Id,
                    BranchId = branch.Id,
                    DefectDescription = request.DefectDescription.Trim(),
                    AccessoriesReceived = request.AccessoriesReceived?.Trim(),
                    TechnicianName = request.TechnicianName?.Trim(),
                    WarrantyExpiryDate = expiry.Value,
                    Status = WarrantyClaimStatus.Open,
                    ClaimDate = now,
                    ProductSerial = serial,
                    SaleDetails = saleDetail,
                    Customer = customer,
                    Branch = branch,
                };

                await _dbContext.WarrantyClaims.AddAsync(claim, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var row = await _dbContext.WarrantyClaims
                    .AsNoTracking()
                    .Where(c => c.Id == claim.Id)
                    .ProjectToRow()
                    .FirstAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = $"Warranty claim {claim.ClaimNumber} opened successfully",
                    Data = row.ToResponse()
                };
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                _logger.LogWarning(
                    ex,
                    "Warranty claim creation lost a uniqueness race for serial {SerialNumber}",
                    serialNumber);

                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 409,
                    Status = "Error",
                    Message = "Another claim was saved at the same moment and took this claim number. Nothing was recorded — please try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating warranty claim for serial {SerialNumber}",
                    serialNumber);

                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the warranty claim."
                };
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

        private Task<string> GenerateClaimNumberAsync(DateTime claimDate, CancellationToken cancellationToken) =>
            DocumentNumbers.NextAsync(
                _dbContext,
                _dbContext.WarrantyClaims.IgnoreQueryFilters().Select(c => c.ClaimNumber),
                $"WC-{BusinessClock.ToLocal(claimDate).Year}-",
                cancellationToken);
    }
}
