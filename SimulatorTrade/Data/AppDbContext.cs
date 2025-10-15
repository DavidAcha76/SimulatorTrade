using Microsoft.EntityFrameworkCore;
using SimulatorTrade.Models;

namespace SimulatorTrade.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<Candle> Candles => Set<Candle>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<TradeFill> TradeFills => Set<TradeFill>();
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<Portfolio> Portfolios => Set<Portfolio>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Índices
            b.Entity<Asset>().HasIndex(x => x.Symbol).IsUnique();
            b.Entity<Candle>().HasIndex(c => new { c.AssetId, c.Time }).IsUnique();
            b.Entity<Position>().HasIndex(p => new { p.PortfolioId, p.AssetId }).IsUnique();

            // Precisiones
            b.Entity<Portfolio>().Property(p => p.Cash).HasPrecision(18, 2);

            b.Entity<Order>().Property(o => o.Price).HasPrecision(18, 4);
            b.Entity<Order>().Property(o => o.Quantity).HasPrecision(18, 4);
            b.Entity<Order>().Property(o => o.FilledQuantity).HasPrecision(18, 4);

            b.Entity<TradeFill>().Property(t => t.Price).HasPrecision(18, 4);
            b.Entity<TradeFill>().Property(t => t.Quantity).HasPrecision(18, 4);

            b.Entity<Position>().Property(p => p.Quantity).HasPrecision(18, 4);
            b.Entity<Position>().Property(p => p.AvgPrice).HasPrecision(18, 4);

            b.Entity<Candle>().Property(c => c.Open).HasPrecision(18, 4);
            b.Entity<Candle>().Property(c => c.High).HasPrecision(18, 4);
            b.Entity<Candle>().Property(c => c.Low).HasPrecision(18, 4);
            b.Entity<Candle>().Property(c => c.Close).HasPrecision(18, 4);

            // ----------------- RELACIONES -----------------
            // IMPORTANTE: nada que apunte a Asset debe tener CASCADE
            b.Entity<Candle>()
             .HasOne(c => c.Asset)
             .WithMany()
             .HasForeignKey(c => c.AssetId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Order>()
             .HasOne(o => o.Asset)
             .WithMany()
             .HasForeignKey(o => o.AssetId)
             .OnDelete(DeleteBehavior.Restrict);

            // Si NO tienes navegación Portfolio.Orders, deja .WithMany()
            b.Entity<Order>()
             .HasOne(o => o.Portfolio)
             .WithMany() // o .WithMany(p => p.Orders) si agregaste esa navegación
             .HasForeignKey(o => o.PortfolioId)
             .OnDelete(DeleteBehavior.Cascade); // esto no causa rutas múltiples hacia TradeFills

            // ÚNICA cascada hacia TradeFills: desde Order
            b.Entity<TradeFill>()
             .HasOne(tf => tf.Order)
             .WithMany() // o .WithMany(o => o.Fills) si tienes la colección
             .HasForeignKey(tf => tf.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            // TradeFill -> Asset: SIN cascada
            b.Entity<TradeFill>()
             .HasOne(tf => tf.Asset)
             .WithMany()
             .HasForeignKey(tf => tf.AssetId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Position>()
             .HasOne(p => p.Asset)
             .WithMany()
             .HasForeignKey(p => p.AssetId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Position>()
             .HasOne(p => p.Portfolio)
             .WithMany(pf => pf.Positions)
             .HasForeignKey(p => p.PortfolioId)
             .OnDelete(DeleteBehavior.Cascade);

            // (Opcional, pero blindaje): fuerza Restrict global y luego re-aplica la única cascada.
            foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                fk.DeleteBehavior = DeleteBehavior.Restrict;

            b.Entity<TradeFill>()
             .HasOne(tf => tf.Order)
             .WithMany()
             .HasForeignKey(tf => tf.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
