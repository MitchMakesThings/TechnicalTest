using Microsoft.AspNetCore.Mvc;
using TechnicalTest.Data.Modules;

namespace TechnicalTest.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController(ICustomerModule customerModule) : BaseController
{
    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var customer = await customerModule.Get(CustomerId);
        if (customer is null)
        {
            return NotFound();
        }
        
        return Ok(customer);
    }
}
