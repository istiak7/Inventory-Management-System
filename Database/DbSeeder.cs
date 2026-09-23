using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared.CurrentUser;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Database
{
    // Puts a working set of permissions, roles and a first admin user in the
    // database so the system can be used right after it is deployed.
    public static class DbSeeder
    {
        // The first admin login comes from configuration (SeedAdmin:Email / SeedAdmin:Password,
        // or the SeedAdmin__Email / SeedAdmin__Password environment variables).
        // Only on a developer machine is there a fallback, so a server never gets a known password.
        private const string DevAdminEmail = "admin@inventory.com";
        private const string DevAdminPassword = "Admin@123";
        private const int MinimumPasswordLength = 8;

        // The permission list and staff defaults live in Shared/CurrentUser/Permissions.cs
        // so the seeder and the endpoints always use the same names.
        private static readonly string[] StaffPermissions = Permissions.StaffDefaults;

        public static async Task SeedAsync(
            AppDbContext db,
            IConfiguration configuration,
            IHostEnvironment environment,
            CancellationToken cancellationToken = default)
        {
            // Apply any pending migrations first so the tables exist.
            await db.Database.MigrateAsync(cancellationToken);

            await SeedPermissionsAsync(db, cancellationToken);
            var adminRoleId = await SeedRolesAsync(db, cancellationToken);
            await SeedAdminUserAsync(db, adminRoleId, configuration, environment, cancellationToken);
        }

        private static async Task SeedPermissionsAsync(AppDbContext db, CancellationToken cancellationToken)
        {
            var existing = await db.Permissions
                .Select(p => p.Name)
                .ToListAsync(cancellationToken);

            var missing = Permissions.All
                .Where(p => !existing.Contains(p.Name))
                .Select(p => new Permission { Name = p.Name, Description = p.Description })
                .ToList();

            if (missing.Count > 0)
            {
                await db.Permissions.AddRangeAsync(missing, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        // Returns the Admin role id so the admin user can be linked to it.
        private static async Task<int> SeedRolesAsync(AppDbContext db, CancellationToken cancellationToken)
        {
            var allPermissions = await db.Permissions
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(cancellationToken);

            // Admin: every permission.
            var admin = await EnsureRoleAsync(
                db, "Admin", "Full access to everything, including all branches",
                allPermissions.Select(p => p.Id), cancellationToken);

            // Staff: a limited day-to-day set, tied to a single branch.
            var staffPermissionIds = allPermissions
                .Where(p => StaffPermissions.Contains(p.Name))
                .Select(p => p.Id);
            await EnsureRoleAsync(
                db, "Staff", "Day-to-day access within a single branch",
                staffPermissionIds, cancellationToken);

            return admin.Id;
        }

        private static async Task<Role> EnsureRoleAsync(
            AppDbContext db,
            string name,
            string description,
            IEnumerable<int> permissionIds,
            CancellationToken cancellationToken)
        {
            var role = await db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

            if (role is null)
            {
                role = new Role { Name = name, Description = description };
                await db.Roles.AddAsync(role, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            // Add any permissions the role does not have yet (never removes).
            var current = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
            var toAdd = permissionIds
                .Where(id => !current.Contains(id))
                .Select(id => new RolePermission { RoleId = role.Id, PermissionId = id })
                .ToList();

            if (toAdd.Count > 0)
            {
                await db.RolePermissions.AddRangeAsync(toAdd, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            return role;
        }

        private static async Task SeedAdminUserAsync(
            AppDbContext db,
            int adminRoleId,
            IConfiguration configuration,
            IHostEnvironment environment,
            CancellationToken cancellationToken)
        {
            // Once any admin exists the seeder never touches users again.
            var adminExists = await db.Users.AnyAsync(u => u.RoleId == adminRoleId, cancellationToken);
            if (adminExists) return;

            var isDevelopment = environment.IsDevelopment();
            var adminEmail = configuration["SeedAdmin:Email"];
            var adminPassword = configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail))
                adminEmail = isDevelopment ? DevAdminEmail : null;
            if (string.IsNullOrWhiteSpace(adminPassword))
                adminPassword = isDevelopment ? DevAdminPassword : null;

            if (adminEmail is null || adminPassword is null || adminPassword.Length < MinimumPasswordLength)
                throw new InvalidOperationException(
                    "There is no admin user yet. Set SeedAdmin__Email and SeedAdmin__Password " +
                    $"(at least {MinimumPasswordLength} characters) so the first admin can be created.");

            var admin = new User
            {
                Name = "Administrator",
                Email = adminEmail.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                RoleId = adminRoleId,
                BranchId = null, // all branches
                IsActive = (int)EntityStatus.Active,
                RefreshToken = Guid.NewGuid().ToString(),
                RefreshTokenExpireTime = DateTime.UtcNow.AddDays(7)
            };

            await db.Users.AddAsync(admin, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
