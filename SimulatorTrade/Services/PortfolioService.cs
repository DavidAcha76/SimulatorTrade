using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Data;
using SimulatorTrade.Models;

public class PortfolioService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PortfolioService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<Portfolio?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Portfolios
                       .Include(x => x.Positions)
                       .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task UpdateCashAsync(int id, decimal delta, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var p = await db.Portfolios.FirstAsync(x => x.Id == id, ct);
        p.Cash += delta;
        await db.SaveChangesAsync(ct);
    }
}
