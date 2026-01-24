using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalTest.API.Authentication;
using TechnicalTest.API.DTOs;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Modules.DTOs;

namespace TechnicalTest.API.Controllers;

// ASSUMPTION: I'm working to the assumption that these endpoints wouldn't be accessed via the (presumably) client-facing mobile app. Ideally these endpoints would run from a separate admin-only service, with it's own authentication etc (And ideally not on the public internet!)
// Either way, I would expect something like [Authorize(Policy = Policies.IsBankStaff)] applied to this controller
[ApiController]
[AllowAnonymous] // In the worst twist imaginable, for the tech test I'm making _admin_ stuff open to all, while regular endpoints require authentication!
[Route("api/admin/customers")]
public class AdminCustomersController(ICustomerAdminModule customerModule, AdminJwtManager adminJwtManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CustomerDto>>>> GetAll()
    {
        var customers = customerModule.GetAll();

        return Ok(new ApiResponse<IEnumerable<CustomerDto>>(true, await customers.ToListAsync()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Add([FromBody] CustomerCreationDto customer)
    {
        var created = await customerModule.Create(customer);
        if (!created.Success)
        {
            // We could log errors to something like Sentry from here for anything particularly unusual (ie, mobile app shouldn't have allowed an empty Name, so log those errors here for follow up)

            var userFriendlyMessage = created.Errors?.Contains(CustomerModificationError.NotFound) == true ? "Not found." : string.Empty;
            userFriendlyMessage += created.Errors?.Contains(CustomerModificationError.InvalidName) == true ? "Invalid name." : string.Empty;
            userFriendlyMessage += created.Errors?.Contains(CustomerModificationError.InvalidDateOfBirth) == true ? "Invalid date of birth." : string.Empty;
        
            return BadRequest(new ApiResponse<CustomerDto>(false, null, userFriendlyMessage, created.Errors?.Select(e => (int)e)));
        }
        
        return Ok(new ApiResponse<CustomerDto>(true, created.Customer!));
    }

    [HttpDelete("{customerId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int customerId)
    {
        var result = await customerModule.Delete(customerId);
        if (!result.Success)
        {
            if (result.Errors?.Contains(CustomerModificationError.NotFound) == true)
            {
                return NotFound(new ApiResponse<bool>(false, false));
            }
            return BadRequest(new ApiResponse<bool>(false, false, "Failed to delete customer",  result.Errors?.Select(e => (int)e)));
        }

        return Ok(new ApiResponse<bool>(true, true));
    }

    // ASSUMPTION: We'd really have a proper login method that customers would use.
    // To save myself time for the tech test though I've left account management out of scope.
    // My default approach would be to use ASP.NET Core Identity.
    // For demonstration of how authentication could work on the customer-facing endpoints I've included this convenience method to generate JWTs.
    // Note that I'm also not returning as an ApiResponse, solely so it can be copied from the swagger output with swagger UI's handy copy-response button.
    [HttpPost("{customerId:int}")]
    public async Task<ActionResult> Login(int customerId)
    {
        if (!await customerModule.Exists(customerId))
        {
            return NotFound();
        }

        var claims = new Claim[]
        {
            new(Claims.CustomerId, customerId.ToString()),
        };
        
        return Ok(adminJwtManager.GenerateJwt(claims));
    }
}