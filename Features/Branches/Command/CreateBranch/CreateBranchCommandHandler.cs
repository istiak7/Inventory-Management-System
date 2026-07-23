using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Branches.Command.CreateBranch
{
    public class CreateBranchCommandHandler(
            IBaseRepository<Branch> _branchRepository,
            ILogger<CreateBranchCommandHandler> _logger
        ) : IRequestHandler<CreateBranchCommand, Result>
    {
        public async Task<Result> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
        {
            var existingBranch = await _branchRepository.GetAsync(
                b => b.Name == request.Name && b.IsActive == (int)EntityStatus.Active,
                cancellationToken: cancellationToken);

            if (existingBranch != null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Branch already exists"
                };
            }

            try
            {
                var branch = new Branch
                {
                    Name = request.Name,
                    Location = request.Location,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    CreatedAt = DateTime.UtcNow,
                };
                await _branchRepository.AddAsync(branch, cancellationToken);
                await _branchRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Branch created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the branch."
                };
            }
        }
    }
}
