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
        private const int MinimumSuperAdminPasswordLength = 12;

        // The permission list and staff defaults live in Shared/CurrentUser/Permissions.cs
        // so the seeder and the endpoints always use the same names.
        private static readonly string[] StaffPermissions = Permissions.StaffDefaults;

        public static async Task SeedAsync(
            AppDbContext db,
            IConfiguration configuration,
            IHostEnvironment environment,
            IReadOnlySet<string>? knownApiRoutes = null,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            // Apply any pending migrations first so the tables exist.
            await db.Database.MigrateAsync(cancellationToken);

            await SeedPermissionsAsync(db, cancellationToken);
            var adminRoleId = await SeedRolesAsync(db, cancellationToken);
            await SeedAdminUserAsync(db, adminRoleId, configuration, environment, cancellationToken);
            // The maintenance team's own account (only when configured, see SeedSuperAdminUserAsync).
            await SeedSuperAdminUserAsync(db, configuration, logger, cancellationToken);

            // The first menu tree (System Setup), only while there is none yet.
            await NavigationSeed.SeedAsync(db, cancellationToken);

            // Default menu actions, the Salesman role and existing roles' grants (once only).
            if (knownApiRoutes is not null && logger is not null)
            {
                await MenuActionSeed.SeedAsync(db, knownApiRoutes, logger, cancellationToken);
                // API routes added since, appended to existing pages' actions (once each).
                await MenuActionSeed.AddRoutesAsync(db, knownApiRoutes, logger, cancellationToken);
                // Pages added since (menu row if missing, its actions, Salesman's grants), once each.
                await MenuActionSeed.AddPagesAsync(db, knownApiRoutes, logger, cancellationToken);
                // Whole actions added to existing pages since (e.g. Sales > Edit Invoice), once each.
                await MenuActionSeed.AddActionsAsync(db, knownApiRoutes, logger, cancellationToken);
                // Actions whose API was removed since (e.g. Sale Void), soft-deleted once each.
                await MenuActionSeed.RemoveActionsAsync(db, logger, cancellationToken);
            }

            // Existing suppliers get their linked customer rows (once only).
            if (logger is not null)
                await PartyLinkSeed.SeedAsync(db, logger, cancellationToken);
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

            // Super Admin: the maintenance team's role. Every permission, like Admin.
            await EnsureRoleAsync(
                db, MenuActionAccess.SuperAdminRoleName,
                "Maintenance team: full access to everything, independent of the Admin accounts",
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

            var isNew = role is null;
            if (role is null)
            {
                role = new Role { Name = name, Description = description };
                await db.Roles.AddAsync(role, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            // Default permissions are given only when the role is first created, so a permission
            // an admin later removed from Staff does not come back on the next restart.
            // Admin and Super Admin are the exception: they always get every permission,
            // including new ones.
            if (!isNew && !MenuActionAccess.IsFullAccessRoleName(name))
                return role;

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

        // The maintenance team's login, separate from the client's Admin accounts (which this
        // never reads or changes). It comes only from configuration:
        //   SeedSuperAdmin:Email, SeedSuperAdmin:Password, optional SeedSuperAdmin:UserName
        //   (environment: SeedSuperAdmin__Email, SeedSuperAdmin__Password, SeedSuperAdmin__UserName).
        // There is no built-in default, so a server never gets a known password.
        //
        // Safe to run on every start: once a Super Admin account exists nothing is created or
        // changed (the password is NOT reset from configuration; change it in the app). A problem
        // here is logged and skipped, never thrown: it must not stop the application starting.
        private static async Task SeedSuperAdminUserAsync(
            AppDbContext db,
            IConfiguration configuration,
            ILogger? logger,
            CancellationToken cancellationToken)
        {
            var roleId = await db.Roles
                .Where(r => r.Name == MenuActionAccess.SuperAdminRoleName)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (roleId is null) return;

            if (await db.Users.AnyAsync(u => u.RoleId == roleId, cancellationToken)) return;

            var email = configuration["SeedSuperAdmin:Email"]?.Trim().ToLowerInvariant();
            var password = configuration["SeedSuperAdmin:Password"];
            var userName = configuration["SeedSuperAdmin:UserName"]?.Trim();
            if (string.IsNullOrWhiteSpace(userName)) userName = "superadmin";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger?.LogInformation(
                    "No Super Admin account exists and SeedSuperAdmin__Email / SeedSuperAdmin__Password are not set; none was created.");
                return;
            }

            if (password.Length < MinimumSuperAdminPasswordLength)
            {
                logger?.LogError(
                    "Super Admin account not created: SeedSuperAdmin__Password must be at least {Length} characters.",
                    MinimumSuperAdminPasswordLength);
                return;
            }

            // Never take over an account that already uses this email or user name.
            var lowerName = userName.ToLowerInvariant();
            var taken = await db.Users.AnyAsync(
                u => u.Email.ToLower() == email || u.Name.ToLower() == lowerName, cancellationToken);
            if (taken)
            {
                logger?.LogError(
                    "Super Admin account not created: another user already has the email or user name set in SeedSuperAdmin.");
                return;
            }

            await db.Users.AddAsync(new User
            {
                Name = userName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = roleId.Value,
                BranchId = null, // all branches
                IsActive = (int)EntityStatus.Active,
                RefreshToken = Guid.NewGuid().ToString(),
                RefreshTokenExpireTime = DateTime.UtcNow.AddDays(7)
            }, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            logger?.LogInformation("Super Admin account '{UserName}' created.", userName);
        }
    }
}
