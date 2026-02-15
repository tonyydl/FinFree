using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FinFree.Api.Controllers;

public abstract class BaseController : ControllerBase
{
    protected int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
