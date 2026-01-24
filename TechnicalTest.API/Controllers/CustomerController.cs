using Microsoft.AspNetCore.Mvc;
using TechnicalTest.API.DTOs;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Modules.DTOs;

namespace TechnicalTest.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController(ICustomerModule customerModule) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Get()
    {
        var customer = await customerModule.Get(CustomerId);
        if (customer is null)
        {
            return ToApiErrorResponse<CustomerDto>("Customer not found",  StatusCodes.Status404NotFound);
        }

        return ToApiResponse(customer);
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update([FromBody] CustomerModificationDto customerUpdate)
    {
        var result = await customerModule.Update(CustomerId, customerUpdate);
        if (!result.Success)
        {
            if (result.Errors?.Contains(CustomerModificationError.NotFound) == true)
            {
                return ToApiErrorResponse<CustomerDto>("Customer not found", StatusCodes.Status404NotFound);
            }

            return ToApiErrorResponse<CustomerDto>(
                "An error occurred while updating the customer",
                StatusCodes.Status400BadRequest,
                result.Errors?.Select(e => (int)e)
            );
        }

        return ToApiResponse(result.Customer!);
    }
}
