using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.MenuSetup.Shared
{
    // Short helpers so every System Setup handler answers in the same Result shape.
    public static class MenuSetupResults
    {
        public static Result Ok(object data, string message) =>
            new() { IsSuccess = true, StatusCode = 200, Status = "Success", Message = message, Data = data };

        public static Result Created(object data, string message) =>
            new() { IsSuccess = true, StatusCode = 201, Status = "Success", Message = message, Data = data };

        public static Result Fail(string message, int statusCode = 400) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message, Data = null! };

        public static Result NotFound(string what) => Fail($"{what} was not found.", 404);
    }
}
