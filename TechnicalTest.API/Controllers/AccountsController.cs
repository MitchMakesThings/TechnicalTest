using Microsoft.AspNetCore.Mvc;
using TechnicalTest.API.DTOs;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Modules.DTOs;

namespace TechnicalTest.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController(IBankAccountModule accountModule) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AccountDto>>>> GetAccounts()
    {
        return ToApiResponse(await accountModule.Get(CustomerId));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountDto?>>> CreateAccount() // ASSUMPTION: No body, as we just generate all details
    {
        var result = await accountModule.Create(CustomerId);
        if (!result.Success)
        {
            if (result.Errors?.Contains(BankAccountModificationError.NoAccountsAvailable) == true)
            {
                return ToApiErrorResponse<AccountDto?>("No accounts available", StatusCodes.Status404NotFound, result.Errors.Select(e => (int)e));
            }

            return ToApiErrorResponse<AccountDto?>("An unknown error occurred.", StatusCodes.Status500InternalServerError, result.Errors?.Select(e => (int)e));
        }

        return ToApiResponse(result.Account);
    }

    [HttpPut("{accountId:int}")]
    public async Task<ActionResult<ApiResponse<AccountDto>>> UpdateAccount(int accountId, [FromBody]AccountDto updatedAccount)
    {
        var result = await accountModule.Update(CustomerId, accountId, updatedAccount);
        if (!result.Success)
        {
            if (result.Errors?.Contains(BankAccountModificationError.NotFound) == true)
            {
                return ToApiErrorResponse<AccountDto>("Not found", StatusCodes.Status404NotFound);
            }
            
            return ToApiErrorResponse<AccountDto>("An unknown error occurred", StatusCodes.Status500InternalServerError, result.Errors?.Select(e => (int)e));
        }

        return ToApiResponse(result.Account!);
    }

    [HttpDelete("{accountId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAccount(int accountId)
    {
        var result = await accountModule.Delete(CustomerId, accountId);
        if (!result.Success)
        {
            if (result.Errors?.Contains(BankAccountModificationError.NotFound) == true)
            {
                return ToApiErrorResponse<bool>("Not found", StatusCodes.Status404NotFound,  result.Errors.Select(e => (int)e));
            }

            if (result.Errors?.Contains(BankAccountModificationError.AccountFrozen) == true)
            {
                return ToApiErrorResponse<bool>("Cannot delete a frozen account", StatusCodes.Status400BadRequest,  result.Errors.Select(e => (int)e));
            }

            if (result.Errors?.Contains(BankAccountModificationError.InvalidBalance) == true)
            {
                return ToApiErrorResponse<bool>("Cannot delete accounts with a non-zero balance", StatusCodes.Status400BadRequest,  result.Errors.Select(e => (int)e));
            }
            
            return ToApiErrorResponse<bool>("An unknown error occurred", StatusCodes.Status500InternalServerError, result.Errors?.Select(e => (int)e));
        }

        return ToApiResponse(true);
    }
}