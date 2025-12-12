using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Prospeo.DbContext.Extensions;
using Prospeo.DbContext.Services;
using Prospeo.DbContext.Models;

namespace Prospeo.DbContext.Examples;

/// <summary>
/// Przyk³ady u¿ycia modelu Firmy i serwisu FirmyService
/// </summary>
public static class FirmyExamples
{
    /// <summary>
    /// Przyk³ad konfiguracji w Program.cs
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Sposób 1: Dodaj wszystkie serwisy Prospeo (zalecany)
        services.AddProspeoServices(configuration);
        
        // Sposób 2: Z niestandardowym connection stringiem
        // services.AddProspeoServices("Server=localhost;Database=ProspeoDb;Trusted_Connection=true;");
        
        // Sposób 3: Tylko DbContext (jeœli chcesz w³asne serwisy)
        // services.AddProspeoDbContext(configuration);
        // services.AddScoped<IFirmyService, FirmyService>();
    }

    /// <summary>
    /// Przyk³ad podstawowego u¿ycia serwisu firm
    /// </summary>
    public static async Task BasicUsageExample(IFirmyService firmyService)
    {
        // Dodawanie nowej firmy
        var nowaFirma = new Firmy
        {
            NazwaFirmy = "Przyk³adowa Firma Sp. z o.o.",
            NazwaBazyERP = "ExampleCompanyDB",
            CzyTestowa = false,
            ApiKey = "api-key-12345",
            AuthorizeAllEndpoints = true
        };

        var dodanaFirma = await firmyService.AddAsync(nowaFirma);
        Console.WriteLine($"Dodano firmê z ID: {dodanaFirma.Id}");

        // Pobieranie firmy po ID
        var firma = await firmyService.GetByIdAsync(dodanaFirma.Id);
        if (firma != null)
        {
            Console.WriteLine($"Nazwa firmy: {firma.NazwaFirmy}");
        }

        // Pobieranie firmy po kluczu API
        var firmaPoApiKey = await firmyService.GetByApiKeyAsync("api-key-12345");
        Console.WriteLine($"Firma znaleziona po API Key: {firmaPoApiKey?.NazwaFirmy}");

        // Pobieranie wszystkich firm testowych
        var firmyTestowe = await firmyService.GetByTestFlagAsync(true);
        Console.WriteLine($"Liczba firm testowych: {firmyTestowe.Count()}");

        // Aktualizacja firmy
        if (firma != null)
        {
            firma.NazwaFirmy = "Zaktualizowana Nazwa Firmy";
            var zaktualizowano = await firmyService.UpdateAsync(firma);
            Console.WriteLine($"Firma zaktualizowana: {zaktualizowano}");
        }

        // Sprawdzenie unikalnoœci klucza API
        var czyUnikalne = await firmyService.IsApiKeyUniqueAsync("nowy-klucz-api");
        Console.WriteLine($"Czy klucz API jest unikalny: {czyUnikalne}");
    }

    /// <summary>
    /// Przyk³ad obs³ugi b³êdów i walidacji
    /// </summary>
    public static async Task ErrorHandlingExample(IFirmyService firmyService)
    {
        try
        {
            // Próba dodania firmy z duplikatem nazwy bazy ERP
            var firma1 = new Firmy
            {
                NazwaFirmy = "Firma 1",
                NazwaBazyERP = "SamaBaza",
                CzyTestowa = false
            };

            var firma2 = new Firmy
            {
                NazwaFirmy = "Firma 2",
                NazwaBazyERP = "SamaBaza", // Duplikat!
                CzyTestowa = false
            };

            await firmyService.AddAsync(firma1);
            await firmyService.AddAsync(firma2); // To rzuci wyj¹tek
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"B³¹d walidacji: {ex.Message}");
        }

        try
        {
            // Próba dodania firmy z duplikatem klucza API
            var firma3 = new Firmy
            {
                NazwaFirmy = "Firma 3",
                NazwaBazyERP = "Baza3",
                ApiKey = "api-key-12345", // Duplikat klucza API
                CzyTestowa = false
            };

            await firmyService.AddAsync(firma3); // To te¿ rzuci wyj¹tek
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"B³¹d walidacji klucza API: {ex.Message}");
        }
    }

    /// <summary>
    /// Przyk³ad wyszukiwania i filtrowania firm
    /// </summary>
    public static async Task SearchExample(IFirmyService firmyService)
    {
        // Pobierz wszystkie firmy
        var wszystkieFirmy = await firmyService.GetAllAsync();
        Console.WriteLine($"Wszystkich firm: {wszystkieFirmy.Count()}");

        // Pobierz tylko firmy produkcyjne
        var firmyProdukcyjne = await firmyService.GetByTestFlagAsync(false);
        Console.WriteLine($"Firm produkcyjnych: {firmyProdukcyjne.Count()}");

        // Pobierz tylko firmy testowe
        var firmyTestowe = await firmyService.GetByTestFlagAsync(true);
        Console.WriteLine($"Firm testowych: {firmyTestowe.Count()}");

        // SprawdŸ czy firma o okreœlonym ID istnieje
        var istnieje = await firmyService.ExistsAsync(1);
        Console.WriteLine($"Firma o ID 1 istnieje: {istnieje}");

        // ZnajdŸ firmê po nazwie bazy ERP
        var firmaPoNazwieBazy = await firmyService.GetByNazwaBazyERPAsync("ExampleCompanyDB");
        if (firmaPoNazwieBazy != null)
        {
            Console.WriteLine($"Znaleziono firmê: {firmaPoNazwieBazy.NazwaFirmy}");
        }
    }
}

/// <summary>
/// Przyk³ad mapowania miêdzy modelami a DTOs
/// </summary>
public static class FirmyMappingExamples
{
    /// <summary>
    /// Mapowanie z modelu do DTO
    /// </summary>
    public static DTOs.FirmaDto MapToDto(Firmy firma)
    {
        return new DTOs.FirmaDto
        {
            Id = firma.Id,
            RowID = firma.RowID,
            NazwaFirmy = firma.NazwaFirmy,
            NazwaBazyERP = firma.NazwaBazyERP,
            CzyTestowa = firma.CzyTestowa,
            MaApiKey = !string.IsNullOrWhiteSpace(firma.ApiKey),
            AuthorizeAllEndpoints = firma.AuthorizeAllEndpoints
        };
    }

    /// <summary>
    /// Mapowanie z CreateDTO do modelu
    /// </summary>
    public static Firmy MapFromCreateDto(DTOs.CreateFirmaDto dto)
    {
        return new Firmy
        {
            NazwaFirmy = dto.NazwaFirmy,
            NazwaBazyERP = dto.NazwaBazyERP,
            CzyTestowa = dto.CzyTestowa,
            ApiKey = dto.ApiKey,
            AuthorizeAllEndpoints = dto.AuthorizeAllEndpoints
        };
    }

    /// <summary>
    /// Aktualizacja modelu z UpdateDTO
    /// </summary>
    public static void UpdateFromDto(Firmy firma, DTOs.UpdateFirmaDto dto)
    {
        firma.NazwaFirmy = dto.NazwaFirmy;
        firma.NazwaBazyERP = dto.NazwaBazyERP;
        firma.CzyTestowa = dto.CzyTestowa;
        firma.ApiKey = dto.ApiKey;
        firma.AuthorizeAllEndpoints = dto.AuthorizeAllEndpoints;
    }
}