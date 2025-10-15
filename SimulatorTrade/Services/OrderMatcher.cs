using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Data;
using SimulatorTrade.Models;

namespace SimulatorTrade.Services
{
    /// <summary>
    /// OrderMatcher como SINGLETON seguro:
    /// - No guarda un DbContext (Scoped) en campos.
    /// - Crea un AppDbContext POR OPERACIÓN usando IDbContextFactory.
    /// - Si algún otro Singleton necesita DB, aplica el mismo patrón.
    /// </summary>
    public class OrderMatcher
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly OrderBook _book;   // ok como Singleton (no depende de Scoped)
        private readonly RiskEngine _risk;  // ok como Singleton (no depende de Scoped)

        /// <summary>
        /// Inyecta la factory en vez de AppDbContext directo.
        /// </summary>
        public OrderMatcher(
            IDbContextFactory<AppDbContext> dbFactory,
            OrderBook book,
            RiskEngine risk)
        {
            _dbFactory = dbFactory;
            _book = book;
            _risk = risk;
        }

        /// <summary>
        /// Intenta casar 'incoming' contra el lado contrario del libro.
        /// Crea su propio DbContext y lo dispone al final.
        /// </summary>
        public async Task<List<TradeFill>> MatchAsync(Order incoming, string symbol, decimal lastPrice)
        {
            // Crea un contexto por operación (seguro para Singletons)
            await using var db = await _dbFactory.CreateDbContextAsync();

            var fills = new List<TradeFill>();
            var (bids, asks) = _book.Get(symbol);
            List<Order> counter = incoming.Side == Side.Buy ? asks : bids;

            // Si 'incoming' y 'other' vienen de memoria y no están trackeados, los adjuntamos.
            EnsureTracked(db, incoming);

            bool Exec(Order other)
            {
                if (incoming.Type == OrderType.Market || other.Type == OrderType.Market) return true;
                if (!incoming.Price.HasValue || !other.Price.HasValue) return true;
                return incoming.Side == Side.Buy
                    ? incoming.Price.Value >= other.Price.Value
                    : incoming.Price.Value <= other.Price.Value;
            }

            foreach (var other in counter.ToList())
            {
                if (incoming.Status == OrderStatus.Filled) break;
                if (other.Status == OrderStatus.Filled || other.PortfolioId == incoming.PortfolioId) continue;
                if (!Exec(other)) continue;

                // Asegura tracking de 'other' si viene de memoria
                EnsureTracked(db, other);

                var px = other.Price ?? incoming.Price ?? lastPrice;
                if (!_risk.CanExecute(incoming, px)) break;

                var qty = Math.Min(incoming.Quantity - incoming.FilledQuantity,
                                   other.Quantity - other.FilledQuantity);
                if (qty <= 0) continue;

                incoming.FilledQuantity += qty;
                other.FilledQuantity += qty;

                var fill = new TradeFill
                {
                    OrderId = incoming.Id,
                    AssetId = incoming.AssetId,
                    Price = px,
                    Quantity = qty
                };
                db.TradeFills.Add(fill);
                fills.Add(fill);

                incoming.Status = incoming.FilledQuantity >= incoming.Quantity
                    ? OrderStatus.Filled
                    : OrderStatus.PartiallyFilled;

                other.Status = other.FilledQuantity >= other.Quantity
                    ? OrderStatus.Filled
                    : OrderStatus.PartiallyFilled;

                await ApplyFill(db, incoming, other, px, qty);

                if (other.Status == OrderStatus.Filled)
                    _book.Remove(other, symbol);
            }

            await db.SaveChangesAsync();
            return fills;
        }

        /// <summary>
        /// Aplica el fill a ambos portafolios y posiciones; usa el mismo DbContext local.
        /// </summary>
        private static async Task ApplyFill(AppDbContext db, Order a, Order b, decimal px, decimal qty)
        {
            var pa = await db.Portfolios
                            .Include(x => x.Positions)
                            .FirstAsync(x => x.Id == a.PortfolioId);

            var pb = await db.Portfolios
                            .Include(x => x.Positions)
                            .FirstAsync(x => x.Id == b.PortfolioId);

            var aid = a.AssetId;

            if (a.Side == Side.Buy)
            {
                UpdatePosition(pa, aid, qty, px, +1);
                UpdatePosition(pb, aid, qty, px, -1);
                pa.Cash -= qty * px;
                pb.Cash += qty * px;
            }
            else
            {
                UpdatePosition(pa, aid, qty, px, -1);
                UpdatePosition(pb, aid, qty, px, +1);
                pa.Cash += qty * px;
                pb.Cash -= qty * px;
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Asegura que una orden en memoria quede trackeada para persistir cambios de estado/cantidades.
        /// </summary>
        private static void EnsureTracked(AppDbContext db, Order o)
        {
            var entry = db.Entry(o);
            if (entry.State == EntityState.Detached)
            {
                // Si la entidad tiene clave asignada, Attach es suficiente.
                // Si fuera nueva, deberías Add en lugar de Attach.
                db.Attach(o);
            }
        }

        /// <summary>
        /// Actualiza la posición manteniendo precio promedio cuando la dirección es positiva.
        /// </summary>
        private static void UpdatePosition(Portfolio p, int assetId, decimal qty, decimal price, int dir)
        {
            var pos = p.Positions.FirstOrDefault(x => x.AssetId == assetId);
            if (pos == null)
            {
                pos = new Position { AssetId = assetId, PortfolioId = p.Id };
                p.Positions.Add(pos);
            }

            var newQty = pos.Quantity + dir * qty;

            if (newQty == 0)
            {
                pos.Quantity = 0;
                pos.AvgPrice = 0;
                return;
            }

            if (dir > 0)
            {
                var total = pos.AvgPrice * pos.Quantity + qty * price;
                pos.Quantity = newQty;
                pos.AvgPrice = total / pos.Quantity;
            }
            else
            {
                pos.Quantity = newQty;
            }
        }
    }
}
