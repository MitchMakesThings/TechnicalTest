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
        try
        {
            return ToApiResponse(await transactionModule.GetTransactions(CustomerId, accountId));
        }
        catch (KeyNotFoundException)
        {
            return ToApiErrorResponse<IEnumerable<TransactionDto>>("Account not found");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TransactionDto>>> Create(TransactionCreationDto newTransaction)
    {
        var result = await transactionModule.Create(
            CustomerId,
            newTransaction.DebitAccountId,
            newTransaction.CreditAccountId,
            newTransaction.Amount,
            newTransaction.Reference
        );

        if (!result.Success)
        {
            return ToApiErrorResponse<TransactionDto>("Invalid request", errorCodes: result.Errors?.Select(e => e.ToString()));
        }
        
        return ToApiResponse(result.Transaction!);
    }
}