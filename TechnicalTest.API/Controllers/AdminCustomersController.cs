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

        return Results.Ok(created);
    }
}