using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Command.UpdateSaleRemarks
{
    public class UpdateSaleRemarksHandler(
        IBaseRepository<CustomerSale> _saleRepository,
        ILogger<UpdateSaleRemarksHandler> _logger
    ) : IRequestHandler<UpdateSaleRemarksCommand, Result>
    {
        public async Task<Result> Handle(UpdateSaleRemarksCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetByIdAsync(request.Id, cancellationToken);
            if (sale is null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Not Found", Message = $"Sale with id {request.Id} was not found." };

            try
            {
                // Blank clears the note; stored as null so readers only check for null.
                sale.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
                sale.UpDatedAt = DateTime.UtcNow;

                await _saleRepository.UpdateAsync(sale, cancellationToken);
                await _saleRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Remarks updated successfully",
                    Data = sale.Remarks
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating remarks for sale {SaleId}", request.Id);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while updating the remarks." };
            }
        }
    }
}
