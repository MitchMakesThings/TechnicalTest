using Microsoft.AspNetCore.Mvc;
using TechnicalTest.API.DTOs;
using TechnicalTest.Data.Modules;
using TechnicalTest.Data.Modules.DTOs;

namespace TechnicalTest.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController(ITransactionModule transactionModule) : BaseController
{
    [HttpGet("{accountId:int}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TransactionDto>>>> GetTransactions(int accountId)
    {
        return ToApiResponse(await transactionModule.GetTransactions(accountId));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Create(TransactionCreationDto newTransaction)
    {
        var result = await transactionModule.Create(
            newTransaction.DebitAccountId,
            newTransaction.CreditAccountId,
            newTransaction.Amount,
            newTransaction.Reference);

        if (!result.Success)
        {
            return ToApiErrorResponse<TransactionDto>("Invalid request", errorCodes: result.Errors?.Select(e => (int)e));
        }
        
        return ToApiResponse(result.Transaction!);
    }
}