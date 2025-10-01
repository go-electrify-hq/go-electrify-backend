using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _svc;

        public UserController(IUserService svc)
        {
            _svc = svc;
        }

        [HttpGet]
        [ProducesResponseType(typeof(UserListPageDto), 200)]
        public async Task<IActionResult> List([FromQuery] UserListQueryDto query, CancellationToken ct)
        {
            var data = await _svc.ListAsync(query, ct);
            return Ok(data);
        }
    }
}
