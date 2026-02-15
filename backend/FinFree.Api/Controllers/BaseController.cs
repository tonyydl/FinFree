using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FinFree.Api.Controllers;

public abstract class BaseController : ControllerBase
{
    protected int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            throw new UnauthorizedAccessException("無效的使用者憑證");
        return userId;
    }
}
