using Inventory_Management_System.Database;
using Inventory_Management_System.Features.SystemSetup.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.SystemSetup.Queries.GetSystemSetupStatus
{
    /// <summary>
    /// Checks every setup section the shop needs before it can buy and sell, like the accounting
    /// portal's CommonSetup/StatusAndData. Sections run one after another on purpose: they share one
    /// scoped DbContext. A section that fails is reported as Error; the others still return.
    /// </summary>
    public class GetSystemSetupStatusQueryHandler(
        AppDbContext _db,
        ILogger<GetSystemSetupStatusQueryHandler> _logger
    ) : IRequestHandler<GetSystemSetupStatusQuery, Result>
    {
        // The section table below is static, so it reaches the context through this field.
        private AppDbContext Db => _db;

        private const int Deleted = (int)EntityStatus.Deleted;
        private const int Active = (int)EntityStatus.Active;

        // The order here is the order of the setup steps on the System Health page.
        private sealed record Section(
            string Key,
            string Name,
            bool IsRequired,
            Func<GetSystemSetupStatusQueryHandler, SetupSectionResponse, CancellationToken, Task> Evaluate);

        private static readonly Section[] Sections =
        [
            new("CompanyInformation", "Company Information", true, (h, s, ct) => h.CompanyAsync(s, ct)),
            new("Branches", "Branches / Warehouses", true, (h, s, ct) => h.CountAsync(s,
                h.Db.Branches.Where(b => b.IsActive != Deleted).Select(b => b.IsActive), "No branch or warehouse has been created yet.", ct)),
            new("Categories", "Product Categories", true, (h, s, ct) => h.CountAsync(s,
                h.Db.ProductCategories.Where(c => c.IsActive != Deleted).Select(c => c.IsActive), "No product category has been created yet.", ct)),
            new("SubCategories", "Product Sub-Categories", true, (h, s, ct) => h.CountAsync(s,
                h.Db.ProductSubCategories.Where(c => c.IsActive != Deleted).Select(c => c.IsActive), "No product sub-category has been created yet.", ct)),
            new("Brands", "Brands", true, (h, s, ct) => h.CountAsync(s,
                h.Db.Brands.Where(b => b.IsActive != Deleted).Select(b => b.IsActive), "No brand has been created yet.", ct)),
            // Variants are what is bought and sold, so they are counted, not product headers.
            new("Products", "Products", true, (h, s, ct) => h.CountAsync(s,
                h.Db.ProductVariants.Where(v => v.IsActive != Deleted).Select(v => v.IsActive), "No product (variant / SKU) has been created yet.", ct)),
            new("Suppliers", "Suppliers", true, (h, s, ct) => h.CountAsync(s,
                h.Db.Suppliers.Where(x => x.IsActive != Deleted).Select(x => x.IsActive), "No supplier has been created yet.", ct)),
            // Optional: walk-in sales need no customer, expenses are optional, and one admin can run a shop.
            new("Customers", "Customers", false, (h, s, ct) => h.CountAsync(s,
                h.Db.Customers.Where(x => x.IsActive != Deleted).Select(x => x.IsActive), "No customer has been created yet.", ct)),
            new("ExpenseCategories", "Expense Categories", false, (h, s, ct) => h.CountAsync(s,
                h.Db.ExpenseCategories.Where(x => x.IsActive != Deleted).Select(x => x.IsActive), "No expense category has been created yet.", ct)),
            new("StaffUsers", "Staff Users", false, (h, s, ct) => h.CountAsync(s,
                h.Db.Users.Where(u => u.IsActive != Deleted && u.Role.Name != "Admin").Select(u => u.IsActive), "Only the administrator account exists.", ct)),
        ];

        public async Task<Result> Handle(GetSystemSetupStatusQuery request, CancellationToken cancellationToken)
        {
            var results = new List<SetupSectionResponse>(Sections.Length);

            for (var i = 0; i < Sections.Length; i++)
            {
                var section = Sections[i];
                var response = new SetupSectionResponse
                {
                    Section = section.Key,
                    Name = section.Name,
                    Order = i + 1,
                    IsRequired = section.IsRequired,
                };

                try
                {
                    await section.Evaluate(this, response, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // The exception text stays in the log; the page only learns that this section failed.
                    _logger.LogWarning(ex, "System setup: failed to check section {Section}", section.Key);
                    response.Status = SetupSectionStatus.Error;
                    response.Message = "Could not check this section.";
                    response.Data = null;
                }

                results.Add(response);
            }

            var required = results.Count(s => s.IsRequired);
            var configured = results.Count(s => s.IsRequired && s.Status == SetupSectionStatus.Configured);
            var overall = configured == required ? SetupOverallStatus.Completed
                : configured == 0 ? SetupOverallStatus.NotStarted
                : SetupOverallStatus.InProgress;

            bool hasTransactions;
            try
            {
                hasTransactions = await _db.CustomerSales.IgnoreQueryFilters().AnyAsync(cancellationToken)
                    || await _db.SupplierPurchases.IgnoreQueryFilters().AnyAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "System setup: failed to check for transactions");
                hasTransactions = false;
            }

            return new Result
            {
                IsSuccess = true,
                StatusCode = 200,
                Status = "Success",
                Message = overall == SetupOverallStatus.Completed
                    ? "System setup is complete."
                    : $"{required - configured} of {required} required setup steps are not complete.",
                Data = new GetSystemSetupStatusResponse(overall, required, configured, hasTransactions, DateTime.UtcNow, results)
            };
        }

        private async Task CompanyAsync(SetupSectionResponse section, CancellationToken cancellationToken)
        {
            var company = await _db.CompanySettings.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync(cancellationToken);
            if (company is null)
            {
                section.Status = SetupSectionStatus.NotConfigured;
                section.Message = "Company information has not been saved yet.";
                return;
            }

            section.Data = CompanySettingResponse.From(company);

            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(company.Name)) missing.Add("Company name");
            if (string.IsNullOrWhiteSpace(company.Address)) missing.Add("Address");
            if (string.IsNullOrWhiteSpace(company.Phone)) missing.Add("Phone");

            section.Status = missing.Count == 0 ? SetupSectionStatus.Configured : SetupSectionStatus.NotConfigured;
            section.Message = missing.Count == 0 ? null : $"Missing: {string.Join(", ", missing)}.";
        }

        // A list section is configured once it has at least one row that is not deleted.
        private async Task CountAsync(SetupSectionResponse section, IQueryable<int> statuses, string emptyMessage, CancellationToken cancellationToken)
        {
            section.TotalRecords = await statuses.CountAsync(cancellationToken);
            section.ActiveRecords = section.TotalRecords == 0 ? 0 : await statuses.CountAsync(s => s == Active, cancellationToken);

            if (section.TotalRecords == 0)
            {
                section.Status = SetupSectionStatus.NotConfigured;
                section.Message = emptyMessage;
            }
            else if (section.ActiveRecords == 0)
            {
                section.Status = SetupSectionStatus.NotConfigured;
                section.Message = "Records exist, but none of them is active.";
            }
            else
            {
                section.Status = SetupSectionStatus.Configured;
                section.Message = null;
            }
        }
    }
}
