using Microsoft.AspNetCore.Mvc;
using TechnicalTest.API.Models;
using TechnicalTest.Data.Modules;

namespace TechnicalTest.API.Controllers;

// TODO presumably an endpoint like this would be in a separate admin-only API with it's own authentication for bank staff
// We're ignoring these problems for a small-scale tech test
[ApiController]
[Route("api/admin/customers")]
public class AdminCustomersController(ICustomerAdminModule customerModule)
{
    [HttpGet]
    public Task<IResult> GetAll()
    {
        var customers =  customerModule.GetAll();

        return Task.FromResult(Results.Ok(customers));
    }

    [HttpPost]
    public async Task<IResult> Add([FromBody] AddCustomerModel customer)
    {
        var created = await customerModule.Create(new CustomerModification(customer.Name, customer.DateOfBirth, customer.DailyLimit));
        if (created.Success)
        {
            return Results.Ok(created.Result);
        }
        
        // We could log errors to something like Sentry from here for anything particularly unusual (ie, mobile app shouldn't have allowed an empty Name, so log those errors here for follow up)

        var userFriendlyMessage = created.Errors?.Any(e => e == CustomerModificationError.NotFound) == true ? "Not found." : string.Empty;
        userFriendlyMessage += created.Errors?.Any(e => e == CustomerModificationError.InvalidName) == true ? "Invalid name." : string.Empty;
        userFriendlyMessage += created.Errors?.Any(e => e == CustomerModificationError.InvalidDateOfBirth) == true ? "Invalid date of birth." : string.Empty;
        
        return Results.Problem(new ProblemDetails()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Customer Creation Failed",
            Detail = userFriendlyMessage,
            Extensions = new Dictionary<string, object>
            {
                ["errors"] = created.Errors ?? []
            }!
        });
    }
}