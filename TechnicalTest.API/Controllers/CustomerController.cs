using Microsoft.AspNetCore.Mvc;
using TechnicalTest.Data.Modules;

namespace TechnicalTest.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController(ICustomerModule customerModule) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<CustomerDto>> Get()
    {
        var customer = await customerModule.Get(CustomerId);
        if (customer is null)
        {
            return NotFound();
        }
        
        return Ok(customer);
    }

    [HttpPut]
    public async Task<ActionResult<CustomerDto>> Update([FromBody] CustomerModificationDto customerUpdate)
    {
        var result = await customerModule.Update(CustomerId, customerUpdate);
        if (!result.Success)
        {
            if (result.Errors?.Contains(CustomerModificationError.NotFound) == true)
            {
                return NotFound();
            }
            // TODO proper error handling for all cases as things evolve
            return Problem(
                "An error occurred while updating the customer.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        return Ok(result.Customer);
    }
}
