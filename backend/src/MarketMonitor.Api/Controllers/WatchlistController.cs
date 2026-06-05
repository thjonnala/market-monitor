using MarketMonitor.Api.Extensions;
using MarketMonitor.Application.Dtos;
using MarketMonitor.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketMonitor.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/watchlist")]
public sealed class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _watchlist;

    public WatchlistController(IWatchlistService watchlist) => _watchlist = watchlist;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WatchlistItemDto>>> Get(CancellationToken ct)
        => Ok(await _watchlist.GetWatchlistAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<WatchlistItemDto>> Add(AddWatchlistRequest request, CancellationToken ct)
    {
        var item = await _watchlist.AddAsync(User.GetUserId(), request.Symbol, ct);
        return CreatedAtAction(nameof(Get), item);
    }

    [HttpDelete("{symbol}")]
    public async Task<IActionResult> Remove(string symbol, CancellationToken ct)
    {
        await _watchlist.RemoveAsync(User.GetUserId(), symbol, ct);
        return NoContent();
    }
}
