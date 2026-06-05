using MarketMonitor.Api.Extensions;
using MarketMonitor.Application.Dtos;
using MarketMonitor.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketMonitor.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolio")]
public sealed class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolio;

    public PortfolioController(IPortfolioService portfolio) => _portfolio = portfolio;

    /// <summary>Get the user's virtual portfolio (creating it with starting cash if needed).</summary>
    [HttpGet]
    public async Task<ActionResult<PortfolioDto>> Get(CancellationToken ct)
        => Ok(await _portfolio.GetOrCreateAsync(User.GetUserId(), ct));

    /// <summary>Virtually buy shares with virtual cash. No real money or orders.</summary>
    [HttpPost("buy")]
    public async Task<ActionResult<PortfolioDto>> Buy(TradeRequest request, CancellationToken ct)
        => Ok(await _portfolio.BuyAsync(User.GetUserId(), request.Symbol, request.Quantity, ct));

    /// <summary>Virtually sell shares you hold.</summary>
    [HttpPost("sell")]
    public async Task<ActionResult<PortfolioDto>> Sell(TradeRequest request, CancellationToken ct)
        => Ok(await _portfolio.SellAsync(User.GetUserId(), request.Symbol, request.Quantity, ct));
}
