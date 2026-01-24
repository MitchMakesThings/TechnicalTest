using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalTest.API.Authentication;

namespace TechnicalTest.API.Controllers;

[Authorize]
public class BaseController : ControllerBase
{
    protected int CustomerId => GetClaimAsInt(Claims.CustomerId) ?? throw new InvalidOperationException("Cannot read customerID claim.");
    
    private int? GetClaimAsInt(string type)
    {
        var claim = User.Claims.SingleOrDefault(c => c.Type == type);
        if (claim == null) return null;

        if (int.TryParse(claim.Value, out var id)) return id;

        return null;
    }
}