using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Customers.Command.UpdateCustomer
{
    // Edits a customer. Works like the supplier edit: the opening balance can only change while
    // the customer has no sales or payments, because every running balance after it would
    // otherwise be wrong.
    public class UpdateCustomerCommandHandler(
        AppDbContext _db,
        ILogger<UpdateCustomerCommandHandler> _logger
    ) : IRequestHandler<UpdateCustomerCommand, Result>
    {
        public async Task<Result> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
            if (customer is null) return Fail(404, $"Customer with id {request.Id} was not found.");

            // The phone number identifies a customer at the sales counter, so it stays unique.
            var phoneNumber = CustomerPhoneNumber.Normalize(request.PhoneNumber);
            if (await _db.Customers.AnyAsync(c => c.PhoneNumber == phoneNumber && c.Id != customer.Id, cancellationToken))
                return Fail(400, "A customer with this phone number already exists.");

            List<CustomerTransaction> openingRows = [];
            if (request.OpeningBalance != customer.OpeningBalance)
            {
                var hasOtherTransactions = await _db.CustomerTransactions
                    .AnyAsync(t => t.CustomerId == customer.Id && t.TransactionType != "Opening", cancellationToken);
                if (hasOtherTransactions)
                    return Fail(400, "The opening balance cannot be changed after sales or payments exist for this customer.");

                openingRows = await _db.CustomerTransactions
                    .Where(t => t.CustomerId == customer.Id && t.TransactionType == "Opening")
                    .ToListAsync(cancellationToken);
            }

            try
            {
                if (request.OpeningBalance != customer.OpeningBalance)
                {
                    _db.CustomerTransactions.RemoveRange(openingRows);
                    customer.OpeningBalance = request.OpeningBalance;
                    if (customer.OpeningBalance != 0)
                        await _db.CustomerTransactions.AddAsync(CustomerTransaction.ForOpening(customer), cancellationToken);
                }

                customer.Group = request.Group;
                customer.Name = request.Name.Trim();
                customer.Description = request.Description;
                customer.PhoneNumber = phoneNumber;
                customer.Email = request.Email;
                customer.Address = request.Address;
                customer.NID = request.NID;
                customer.UpDatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Customer updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", request.Id);
                return Fail(500, "An error occurred while updating the customer.");
            }
        }

        private static Result Fail(int statusCode, string message) => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Status = "Error",
            Message = message
        };
    }
}
