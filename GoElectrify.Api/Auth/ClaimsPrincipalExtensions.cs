using System.Security.Claims;

namespace GoElectrify.Api.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            if (user?.Identity is null || !user.Identity.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            var id = user.FindFirstValue("sub") ?? user.FindFirstValue("uid");

            if (string.IsNullOrWhiteSpace(id) || !int.TryParse(id, out var userId))
                throw new InvalidOperationException("User id not found in token claims (expected int).");

            return userId;
        }

        public static bool TryGetUserId(this ClaimsPrincipal user, out int userId)
        {
            userId = default;
            if (user?.Identity is null || !user.Identity.IsAuthenticated) return false;

            var id = user.FindFirstValue("sub") ?? user.FindFirstValue("uid");

            return int.TryParse(id, out userId);
        }
    }
}
