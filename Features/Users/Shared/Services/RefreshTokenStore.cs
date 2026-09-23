using Inventory_Management_System.Database;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Users.Shared.Services
{
    /// <summary>Signs a user out of their devices by removing their refresh tokens.</summary>
    public static class RefreshTokenStore
    {
        /// <summary>
        /// Removes every refresh token of the user, except the one given (the device making the
        /// request, e.g. the one that just changed its password, stays signed in).
        /// Call SaveChanges afterwards.
        /// </summary>
        public static async Task RevokeAllAsync(
            AppDbContext db,
            int userId,
            string? keepRefreshToken,
            CancellationToken cancellationToken)
        {
            var keepHash = string.IsNullOrWhiteSpace(keepRefreshToken)
                ? null
                : TokenService.HashRefreshToken(keepRefreshToken);

            var tokens = await db.UserRefreshTokens
                .Where(t => t.UserId == userId && t.TokenHash != keepHash)
                .ToListAsync(cancellationToken);

            db.UserRefreshTokens.RemoveRange(tokens);
        }
    }
}
