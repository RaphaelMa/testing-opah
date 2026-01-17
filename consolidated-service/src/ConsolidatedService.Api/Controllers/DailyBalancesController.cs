using Microsoft.AspNetCore.Mvc;
using ConsolidatedService.Application.DTOs;
using ConsolidatedService.Application.UseCases;

namespace ConsolidatedService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DailyBalancesController : ControllerBase
{
    private readonly GetDailyBalanceUseCase _getDailyBalanceUseCase;
    private readonly GetDailyBalanceRangeUseCase _getDailyBalanceRangeUseCase;
    private readonly ILogger<DailyBalancesController> _logger;

    public DailyBalancesController(
        GetDailyBalanceUseCase getDailyBalanceUseCase,
        GetDailyBalanceRangeUseCase getDailyBalanceRangeUseCase,
        ILogger<DailyBalancesController> logger)
    {
        _getDailyBalanceUseCase = getDailyBalanceUseCase;
        _getDailyBalanceRangeUseCase = getDailyBalanceRangeUseCase;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetDailyBalance(
        [FromQuery] Guid merchantId,
        [FromQuery] string? date = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (merchantId == Guid.Empty)
            {
                return BadRequest(new { error = "merchantId is required" });
            }

            if (!string.IsNullOrEmpty(date))
            {
                if (!DateOnly.TryParse(date, out var balanceDate))
                {
                    return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD" });
                }

                _logger.LogInformation("Querying daily balance. MerchantId: {MerchantId}, Date: {Date}", merchantId, balanceDate);

                var result = await _getDailyBalanceUseCase.ExecuteAsync(merchantId, balanceDate, cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning("Daily balance not found. MerchantId: {MerchantId}, Date: {Date}", merchantId, balanceDate);
                    return NotFound(new { error = "Daily balance not found for the specified merchant and date" });
                }

                return Ok(result);
            }

            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (!DateOnly.TryParse(startDate, out var start))
                {
                    return BadRequest(new { error = "Invalid startDate format. Use YYYY-MM-DD" });
                }

                if (!DateOnly.TryParse(endDate, out var end))
                {
                    return BadRequest(new { error = "Invalid endDate format. Use YYYY-MM-DD" });
                }

                if (start > end)
                {
                    return BadRequest(new { error = "startDate must be less than or equal to endDate" });
                }

                _logger.LogInformation("Querying daily balance range. MerchantId: {MerchantId}, StartDate: {StartDate}, EndDate: {EndDate}", merchantId, start, end);

                var results = await _getDailyBalanceRangeUseCase.ExecuteAsync(merchantId, start, end, cancellationToken);

                return Ok(results);
            }

            return BadRequest(new { error = "Either 'date' or both 'startDate' and 'endDate' must be provided" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily balance");
            return StatusCode(500, new { error = "An error occurred while retrieving the daily balance" });
        }
    }
}
