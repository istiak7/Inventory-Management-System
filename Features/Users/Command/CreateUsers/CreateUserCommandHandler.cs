using MediatR;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Users.Command.Events;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;

namespace Inventory_Management_System.Features.Users.Command.CreateUsers
{
    public class CreateUserCommandHandler(IBaseRepository<User> _userRepository, IMediator _mediator)
    : IRequestHandler<CreateUserCommand, Result>
    {
        private string RefreshToken => Guid.NewGuid().ToString();
        public async Task<Result> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userRepository.GetAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "User already exists"
                };
            }
            try
            {
                await _userRepository.AddAsync(new User
                {
                    Name = request.Username,
                    Email = request.Email,
                    PasswordHash = request.Password,
                    RoleId = request.RoleId,
                    RefreshToken = RefreshToken,
                    RefreshTokenExpireTime = DateTime.UtcNow.AddDays(7)
                });
                await _userRepository.SaveChangesAsync();

                var userCreatedEvent = new UserRegistrationEvent
                (
                    existingUser?.Id ?? 0,
                    request.Username,
                    request.Email
                );
                await _mediator.Publish(userCreatedEvent, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "User registered successfully"
                };
            }
            catch (Exception ex)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = $"An error occurred while registering the user: {ex.Message}"
                };
            }
        }
    }
}
