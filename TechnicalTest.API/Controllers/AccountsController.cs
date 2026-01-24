using Microsoft.AspNetCore.Mvc;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Modules.DTOs;

namespace TechnicalTest.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController(IBankAccountModule accountModule) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountDto>>> GetAccounts()
    {
        return Ok(await accountModule.Get(CustomerId));
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount() // ASSUMPTION: No body, as we just generate all details
    {
        var result = await accountModule.Create(CustomerId);
        if (!result.Success)
        {
            if (result.Errors?.Contains(BankAccountModificationError.NoAccountsAvailable) == true)
            {
                return Problem("No accounts available", null, StatusCodes.Status400BadRequest);
            }
            
            return Problem("An unknown error occurred", null, StatusCodes.Status500InternalServerError);
        }
        
        return Ok(result.Account);
    }

    [HttpDelete("{accountId:int}")]
    public async Task<ActionResult<IEnumerable<AccountDto>>> DeleteAccount(int accountId)
    {
        var result = await accountModule.Delete(CustomerId, accountId);
        if (!result.Success)
        {
            if (result.Errors?.Contains(BankAccountModificationError.NotFound) == true)
            {
                return NotFound();
            }

            if (result.Errors?.Contains(BankAccountModificationError.AccountFrozen) == true)
            {
                return Problem("Cannot delete a frozen account", null, StatusCodes.Status400BadRequest);
            }

            if (result.Errors?.Contains(BankAccountModificationError.InvalidBalance) == true)
            {
                return Problem("Cannot delete accounts with a non-zero balance", null, StatusCodes.Status400BadRequest);
            }
            
            return Problem("An unknown error occurred",  null, StatusCodes.Status500InternalServerError);
        }
        
        // Return all accounts, so the mobile app UI can update
        return await GetAccounts();
    }
}