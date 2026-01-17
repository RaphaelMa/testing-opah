using Microsoft.AspNetCore.Mvc;
using TransactionsService.Application.DTOs;
using TransactionsService.Application.UseCases;

namespace TransactionsService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly CreateTransactionUseCase _createTransactionUseCase;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        CreateTransactionUseCase createTransactionUseCase,
        ILogger<TransactionsController> logger)
    {
        _createTransactionUseCase = createTransactionUseCase;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _createTransactionUseCase.ExecuteAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetTransaction),
                new { id = response.Id },
                response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request data");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            return StatusCode(500, new { error = "An error occurred while creating the transaction" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(Guid id)
    {
        return NotFound();
    }
}
