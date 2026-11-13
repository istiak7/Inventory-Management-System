using Inventory_Management_System.Features.DataUpload.Shared.Importers;
using Inventory_Management_System.Shared;
using System.Security.Claims;

namespace Inventory_Management_System.Features.DataUpload.Shared
{
    // The list of upload types, in the order the dropdown shows them.
    // Each importer owns its column list, so the columns live next to the code that reads them.
    public static class DataUploadCatalog
    {
        public static readonly IReadOnlyList<UploadTypeDefinition> All =
        [
            new(DataUploadType.Brand, "Brand",
                "Product brands, e.g. Samsung, Walton.",
                Permissions.ProductsManage, BrandUploadImporter.Columns),

            new(DataUploadType.Category, "Product Category",
                "Top-level product categories.",
                Permissions.ProductsManage, CategoryUploadImporter.Columns),

            new(DataUploadType.SubCategory, "Product Sub Category",
                "Sub-categories, linked to an existing category by its name.",
                Permissions.ProductsManage, SubCategoryUploadImporter.Columns),

            new(DataUploadType.Product, "Product & Variant",
                "One row per variant. Rows with the same product name, sub-category and brand become one product.",
                Permissions.ProductsManage, ProductUploadImporter.Columns),

            new(DataUploadType.Supplier, "Supplier",
                "Suppliers with their opening balance (amount you owe them).",
                Permissions.SuppliersManage, SupplierUploadImporter.Columns),

            new(DataUploadType.Customer, "Customer",
                "Customers with their opening balance (amount they owe you).",
                Permissions.CustomersManage, CustomerUploadImporter.Columns),

            // The Office Expense screens have no permission of their own, so neither does this upload.
            new(DataUploadType.ExpenseCategory, "Expense Category",
                "Headings that office expenses are recorded under.",
                null, ExpenseCategoryUploadImporter.Columns),
        ];

        public static UploadTypeDefinition? Find(int type) =>
            All.FirstOrDefault(d => (int)d.Type == type);

        // Same rule as RequirePermission: admins may do everything, others need the permission claim.
        public static bool CanUpload(ClaimsPrincipal user, UploadTypeDefinition definition) =>
            definition.Permission is null
            || user.IsInRole("Admin")
            || user.Claims.Any(c => c.Type == "permission" && c.Value == definition.Permission);

        public static IResult Forbidden(UploadTypeDefinition definition) =>
            Results.Json(
                new Result
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Status = "Forbidden",
                    Message = $"You do not have permission to upload {definition.Name} data ({definition.Permission})."
                },
                statusCode: StatusCodes.Status403Forbidden);

        public static IResult UnknownType(int type) =>
            Results.Ok(new Result
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Status = "Error",
                Message = $"'{type}' is not a valid upload type."
            });
    }
}
