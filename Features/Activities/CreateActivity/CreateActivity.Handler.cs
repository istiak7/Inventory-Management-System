using MediatR;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Activities.CreateActivity
{
    public class CreateActivityHandler : IRequestHandler<CreateActivityCommand, Result>
    {
        private readonly AppDbContext _appDbContext;
        public CreateActivityHandler(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<Result> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var activity = new Activity
                {
                    Name = request.Title,
                    Description = request.Description
                };
                _appDbContext.Activities.Add(activity);
                await _appDbContext.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Activity created successfully",
                    Data = activity
                };
            }
            catch (Exception ex)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = $"An error occurred while creating the activity: {ex.Message}",
                    Data = null
                };
            }

        }
    }
}
