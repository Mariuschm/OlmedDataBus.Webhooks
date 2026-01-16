using Microsoft.EntityFrameworkCore;
using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Data;

/// <summary>
/// Kontekst bazy danych dla systemu Prospeo
/// </summary>
public class ProspeoDataContext : Microsoft.EntityFrameworkCore.DbContext
{
    /// <summary>
    /// Konstruktor z opcjami DbContext (standardowy)
    /// </summary>
    public ProspeoDataContext(DbContextOptions<ProspeoDataContext> options) : base(options)
    {
    }

    /// <summary>
    /// Konstruktor z bezpoœrednim connection stringiem
    /// </summary>
    public ProspeoDataContext(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Konstruktor bezparametrowy (dla migracji EF)
    /// </summary>
    public ProspeoDataContext()
    {
    }

    /// <summary>
    /// Connection string do bazy danych
    /// </summary>
    public string? ConnectionString { get; }

    /// <summary>
    /// Tabela firm
    /// </summary>
    public DbSet<Firmy> Firmy { get; set; } = null!;

    /// <summary>
    /// Tabela statusów kolejki
    /// </summary>
    public DbSet<QueueStatus> QueueStatuses { get; set; } = null!;

    /// <summary>
    /// Tabela kolejki zadañ
    /// </summary>
    public DbSet<Queue> Queues { get; set; } = null!;

    /// <summary>
    /// Tabela relacji miêdzy elementami kolejki
    /// </summary>
    public DbSet<QueueRelations> QueueRelations { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Jeœli opcje nie zosta³y skonfigurowane i mamy connection string
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(ConnectionString))
        {
            optionsBuilder.UseSqlServer(ConnectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });
        }

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfiguracja dla tabeli Firmy
        modelBuilder.Entity<Firmy>(entity =>
        {
            // Nazwa tabeli i schemat
            entity.ToTable("Firmy", "ProRWS");

            // Klucz g³ówny
            entity.HasKey(e => e.Id)
                .HasName("PK_Firmy");

            // Konfiguracja RowID z domyœln¹ wartoœci¹
            entity.Property(e => e.RowID)
                .HasDefaultValueSql("NEWID()");

            // Indeksy (opcjonalne dla wydajnoœci)
            entity.HasIndex(e => e.RowID)
                .IsUnique()
                .HasDatabaseName("IX_Firmy_RowID");

            entity.HasIndex(e => e.NazwaBazyERP)
                .HasDatabaseName("IX_Firmy_NazwaBazyERP");

            entity.HasIndex(e => e.ApiKey)
                .HasDatabaseName("IX_Firmy_ApiKey")
                .HasFilter("[ApiKey] IS NOT NULL");

            // Dodatkowe walidacje
            entity.Property(e => e.NazwaFirmy)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.NazwaBazyERP)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ApiKey)
                .HasMaxLength(255);

            // Relacja do kolejki (one-to-many)
            entity.HasMany(e => e.QueueItems)
                .WithOne(q => q.Firma)
                .HasForeignKey(q => q.FirmaId)
                .HasConstraintName("FK_Queue_Firmy")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Konfiguracja dla tabeli QueueStatusEnum
        modelBuilder.Entity<QueueStatus>(entity =>
        {
            // Nazwa tabeli i schemat
            entity.ToTable("QueueStatus", "ProRWS");

            // Klucz g³ówny
            entity.HasKey(e => e.Id)
                .HasName("PK_QueueStatus");

            // Konfiguracja RowID z domyœln¹ wartoœci¹
            entity.Property(e => e.RowID)
                .HasDefaultValueSql("NEWID()");

            // Unikalne ograniczenie dla Value
            entity.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("UQ_QueueStatusValue");

            // Indeksy dla wydajnoœci
            entity.HasIndex(e => e.RowID)
                .IsUnique()
                .HasDatabaseName("IX_QueueStatus_RowID");

            entity.HasIndex(e => e.Name)
                .HasDatabaseName("IX_QueueStatus_Name");

            // Konfiguracja w³aœciwoœci
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(16)
                .HasColumnType("varchar(16)");

            entity.Property(e => e.Value)
                .IsRequired();

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(1024)
                .HasColumnType("varchar(1024)");
        });

        // Konfiguracja dla tabeli Queue
        modelBuilder.Entity<Queue>(entity =>
        {
            // Nazwa tabeli i schemat
            entity.ToTable("Queue", "ProRWS");

            // Klucz g³ówny
            entity.HasKey(e => e.Id)
                .HasName("PK_Queue");

            // Konfiguracja RowID z domyœln¹ wartoœci¹
            entity.Property(e => e.RowID)
                .HasDefaultValueSql("NEWID()");

            // Indeksy dla wydajnoœci
            entity.HasIndex(e => e.RowID)
                .IsUnique()
                .HasDatabaseName("IX_Queue_RowID");

            entity.HasIndex(e => e.FirmaId)
                .HasDatabaseName("IX_Queue_Firma");

            entity.HasIndex(e => e.Scope)
                .HasDatabaseName("IX_Queue_Scope");

            entity.HasIndex(e => e.Flg)
                .HasDatabaseName("IX_Queue_Flg");

            entity.HasIndex(e => e.DateAdd)
                .HasDatabaseName("IX_Queue_DateAdd");

            entity.HasIndex(e => e.TargetID)
                .HasDatabaseName("IX_Queue_TargetID");

            // Konfiguracja w³aœciwoœci
            entity.Property(e => e.FirmaId)
                .IsRequired();

            entity.Property(e => e.Scope)
                .IsRequired();

            entity.Property(e => e.Request)
                .IsRequired()
                .HasColumnType("varchar(max)");

            entity.Property(e => e.DateAdd)
                .IsRequired();

            entity.Property(e => e.DateMod)
                .IsRequired();

            entity.Property(e => e.Flg)
                .IsRequired();

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(1024)
                .HasColumnType("varchar(1024)");

            entity.Property(e => e.TargetID)
                .IsRequired();

            // Relacja do firmy (many-to-one)
            entity.HasOne(e => e.Firma)
                .WithMany(f => f.QueueItems)
                .HasForeignKey(e => e.FirmaId)
                .HasConstraintName("FK_Queue_Firmy")
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Konfiguracja dla tabeli QueueRelations
        modelBuilder.Entity<QueueRelations>(entity =>
        {
            // Nazwa tabeli i schemat
            entity.ToTable("QueueRelations", "ProRWS");

            // Klucz g³ówny
            entity.HasKey(e => e.Id)
                .HasName("PK_QueueRelations");

            // Unikalne ograniczenie dla pary SourceItemId-TargetItemId
            entity.HasIndex(e => new { e.SourceItemId, e.TargetItemId })
                .IsUnique()
                .HasDatabaseName("UQ_QueueRelations_Source_Target");

            // Indeks dla SourceItemId (dla szybszego wyszukiwania relacji wychodz¹cych)
            entity.HasIndex(e => e.SourceItemId)
                .HasDatabaseName("IX_QueueRelations_SourceItemId");

            // Indeks dla TargetItemId (dla szybszego wyszukiwania relacji przychodz¹cych)
            entity.HasIndex(e => e.TargetItemId)
                .HasDatabaseName("IX_QueueRelations_TargetItemId");

            // Konfiguracja w³aœciwoœci
            entity.Property(e => e.SourceItemId)
                .IsRequired();

            entity.Property(e => e.TargetItemId)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSDATETIME()");

            // Relacja do Ÿród³owego elementu kolejki (many-to-one)
            entity.HasOne(e => e.SourceItem)
                .WithMany(q => q.SourceRelations)
                .HasForeignKey(e => e.SourceItemId)
                .HasConstraintName("FK_QueueRelations_Source")
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete loops

            // Relacja do docelowego elementu kolejki (many-to-one)
            entity.HasOne(e => e.TargetItem)
                .WithMany(q => q.TargetRelations)
                .HasForeignKey(e => e.TargetItemId)
                .HasConstraintName("FK_QueueRelations_Target")
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete loops
        });
    }
}