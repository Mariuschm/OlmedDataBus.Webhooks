using Microsoft.EntityFrameworkCore;
using Prospeo.Webservice.Queue.Models;

namespace Prospeo.Webservice.Queue.Data;

/// <summary>
/// Kontekst bazy danych dla kolejki zadañ
/// </summary>
public class QueueDbContext : DbContext
{
    public QueueDbContext(DbContextOptions<QueueDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Elementy kolejki
    /// </summary>
    public DbSet<QueueItem> QueueItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<QueueItem>(entity =>
        {
            entity.ToTable("QueueItems");

            // Indeksy dla wydajnoœci
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_QueueItems_Status");

            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAt })
                .HasDatabaseName("IX_QueueItems_Processing");

            entity.HasIndex(e => e.CorrelationId)
                .HasDatabaseName("IX_QueueItems_CorrelationId")
                .HasFilter("[CorrelationId] IS NOT NULL");

            entity.HasIndex(e => e.Type)
                .HasDatabaseName("IX_QueueItems_Type");

            entity.HasIndex(e => e.NextRetryAt)
                .HasDatabaseName("IX_QueueItems_NextRetryAt")
                .HasFilter("[NextRetryAt] IS NOT NULL");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_QueueItems_CreatedAt");

            // Domyœlne wartoœci
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.Status)
                .HasConversion<int>();

            // Ograniczenia
            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Payload)
                .IsRequired();

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100);

            entity.Property(e => e.Source)
                .HasMaxLength(200);
        });
    }
}