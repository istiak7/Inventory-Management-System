using Inventory_Management_System.Features.DataUpload.Shared;
using Inventory_Management_System.Features.DataUpload.Shared.Importers;
using Inventory_Management_System.Features.Users.Shared.Services;
using Inventory_Management_System.Shared.CurrentUser;

namespace Inventory_Management_System.Shared.Extensions.DependencyExtensions
{
    public static class RegisterService
    {
        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenService, TokenService>();

            // Reads the logged-in user from the request; used for branch filtering.
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUserService>();

            // Excel data upload: one importer per upload type, picked by DataUploadRunner.
            services.AddScoped<DataUploadRunner>();
            services.AddScoped<IUploadImporter, BrandUploadImporter>();
            services.AddScoped<IUploadImporter, CategoryUploadImporter>();
            services.AddScoped<IUploadImporter, SubCategoryUploadImporter>();
            services.AddScoped<IUploadImporter, ProductUploadImporter>();
            services.AddScoped<IUploadImporter, SupplierUploadImporter>();
            services.AddScoped<IUploadImporter, CustomerUploadImporter>();
            services.AddScoped<IUploadImporter, ExpenseCategoryUploadImporter>();
        }
    }
}
