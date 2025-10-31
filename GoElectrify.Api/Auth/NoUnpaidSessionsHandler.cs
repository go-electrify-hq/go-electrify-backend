using System.Security.Claims;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.Api.Auth
{
    public sealed class NoUnpaidSessionsHandler : AuthorizationHandler<NoUnpaidSessionsRequirement>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<NoUnpaidSessionsHandler> _log;
        public NoUnpaidSessionsHandler(AppDbContext db, ILogger<NoUnpaidSessionsHandler> log)
        { _db = db; _log = log; }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NoUnpaidSessionsRequirement requirement)
        {
            var uid = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue("uid");
            if (!int.TryParse(uid, out var userId)) return;

            var hasUnpaid = await _db.ChargingSessions
                .Include(s => s.Booking)
                .AnyAsync(s => s.Status == "UNPAID" && s.BookingId != null && s.Booking!.UserId == userId);

            if (!hasUnpaid) context.Succeed(requirement);
            else _log.LogInformation("User {UserId} blocked due to UNPAID sessions", userId);
        }
    }
}
