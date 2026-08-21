using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Command.CreateCustomer
{
    public class CreateCustomerCommandHandler(
        IBaseRepository<Customer> _customerRepository,
        ILogger<CreateCustomerCommandHandler> _logger
    ) : IRequestHandler<CreateCustomerCommand, Result>
    {
        public async Task<Result> Handle(
            CreateCustomerCommand request,
            CancellationToken cancellationToken)
        {
            var phoneNumber = CustomerPhoneNumber.Normalize(request.PhoneNumber);

            var existingCustomer = await _customerRepository.GetAsync(
                c => c.PhoneNumber == phoneNumber,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            if (existingCustomer != null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "A customer with this phone number already exists."
                };
            }

            try
            {
                var customer = new Customer
                {
                    Group = request.Group,
                    Name = request.Name.Trim(),
                    Description = request.Description,
                    PhoneNumber = phoneNumber,
                    Email = request.Email,
                    Address = request.Address,
                    NID = request.NID,
                    OpeningBalance = request.OpeningBalance,
                    CreatedAt = DateTime.UtcNow,
                };

                await _customerRepository.AddAsync(customer, cancellationToken);
                await _customerRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Customer created successfully",
                    Data = customer.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the customer."
                };
            }
        }
    }
}
