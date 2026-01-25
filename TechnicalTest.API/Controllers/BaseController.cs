using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalTest.API.Authentication;
using TechnicalTest.API.DTOs;

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
    
    public ActionResult<ApiResponse<T>> ToApiResponse<T>(T data)
    {
        return Ok(new ApiResponse<T>(true, data));
    }
    
    public ActionResult<ApiResponse<T>> ToApiErrorResponse<T>(string message, int statusCode = 400, IEnumerable<string>? errorCodes = null)
    {
        return StatusCode(statusCode, new ApiResponse<T>(false, default, message, errorCodes));
    }
}