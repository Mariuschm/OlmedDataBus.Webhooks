using System.Text.Json.Serialization;

namespace Prosepo.Webhooks.Models
{
    /// <summary>
    /// ¯¹danie wykonania zadania HTTP - mo¿e byæ u¿yte zarówno dla jednorazowych zadañ jak i zadañ cyklicznych.
    /// Obs³uguje ró¿ne metody HTTP, nag³ówki, zawartoœæ oraz automatyczn¹ autoryzacjê Olmed.
    /// </summary>
    public class CronJobRequest
    {
        /// <summary>
        /// Metoda HTTP do wykonania (GET, POST, PUT, DELETE, itp.
        /// Domyœlnie: GET
        /// </summary>
        /// <example>POST</example>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// URL docelowy do wykonania ¿¹dania.
        /// Musi byæ pe³ny URL z protoko³em (http:// lub https://).
        /// </summary>
        /// <example>https://api.example.com/data</example>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Opcjonalne nag³ówki HTTP do dodania do ¿¹dania.
        /// Content-Type jest obs³ugiwany automatycznie dla ¿¹dañ z body.
        /// </summary>
        /// <example>{"User-Agent": "Prosepo-Cron/1.0", "Accept": "application/json"}</example>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Zawartoœæ ¿¹dania dla metod POST/PUT.
        /// Zazwyczaj JSON, ale mo¿e byæ dowolny string.
        /// </summary>
        /// <example>{"message": "Hello World", "timestamp": "2024-01-01T10:00:00Z"}</example>
        public string? Body { get; set; }

        /// <summary>
        /// Czy automatycznie dodaæ token autoryzacji Olmed dla ¿¹dañ do domeny grupaolmed.pl.
        /// Jeœli true i URL zawiera "grupaolmed.pl", automatycznie zostanie dodany nag³ówek Authorization.
        /// </summary>
        /// <example>true</example>
        public bool UseOlmedAuth { get; set; } = false;
    }

    /// <summary>
    /// OdpowiedŸ z wykonania zadania HTTP.
    /// Zawiera informacje o sukcesie, statusie HTTP, odpowiedzi oraz czasie wykonania.
    /// </summary>
    public class CronJobResponse
    {
        /// <summary>
        /// Czy zadanie zakoñczy³o siê sukcesem (status HTTP 2xx).
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Kod statusu HTTP z odpowiedzi (200, 404, 500, itp.).
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Pe³na odpowiedŸ z serwera jako string.
        /// Mo¿e byæ JSON, HTML, XML lub dowolny inny format.
        /// </summary>
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// Czas wykonania zadania (UTC).
        /// </summary>
        public DateTime ExecutedAt { get; set; }
    }

    /// <summary>
    /// OdpowiedŸ z API Olmed na ¿¹dania autoryzacji (login/refresh).
    /// Mapuje na format JSON zwracany przez API Olmed.
    /// </summary>
    public class OlmedAuthResponse
    {
        /// <summary>
        /// Token dostêpu JWT z API Olmed.
        /// Mapowany z pola "access_token" w odpowiedzi JSON.
        /// </summary>
        [JsonPropertyName("access_token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Czas wygaœniêcia tokena w sekundach.
        /// Mapowany z pola "expires_in" w odpowiedzi JSON.
        /// Zazwyczaj 3600 sekund (1 godzina).
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        /// <summary>
        /// Typ tokena, zazwyczaj "bearer".
        /// Mapowany z pola "token_type" w odpowiedzi JSON.
        /// </summary>
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ujednolicona odpowiedŸ dla operacji autoryzacji.
    /// U¿ywana do zwracania informacji o tokenach w API kontrolera.
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// Czy operacja autoryzacji zakoñczy³a siê sukcesem.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Token autoryzacji (jeœli operacja zakoñczona sukcesem).
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Czas wygaœniêcia tokena (UTC).
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Pozosta³y czas wa¿noœci tokena w sekundach.
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Wiadomoœæ opisuj¹ca wynik operacji.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Wewnêtrzny model przechowuj¹cy informacje o tokenie w storage.
    /// U¿ywany do zarz¹dzania tokenami w pamiêci aplikacji.
    /// </summary>
    public class TokenInfo
    {
        /// <summary>
        /// Token JWT otrzymany z API Olmed.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Czas wygaœniêcia tokena (UTC).
        /// Obliczany na podstawie ExpiresIn z odpowiedzi API.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Czas utworzenia/otrzymania tokena (UTC).
        /// U¿ywany do statystyk i debugowania.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// ¯¹danie zaplanowania zadania cyklicznego.
    /// Zawiera identyfikator zadania oraz jego harmonogram.
    /// </summary>
    public class ScheduleJobRequest
    {
        /// <summary>
        /// Unikalny identyfikator zadania.
        /// U¿ywany do zarz¹dzania zadaniem (aktualizacja, usuwanie, monitorowanie).
        /// </summary>
        /// <example>daily-backup-job</example>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Harmonogram wykonywania zadania.
        /// Definiuje kiedy i jak czêsto zadanie ma byæ wykonywane.
        /// </summary>
        public CronJobSchedule Schedule { get; set; } = new();
    }

    /// <summary>
    /// Harmonogram zadania cyklicznego.
    /// Obs³uguje ró¿ne typy harmonogramów: interwa³y, dzienne, tygodniowe, miesiêczne i wyra¿enia cron.
    /// </summary>
    public class CronJobSchedule
    {
        /// <summary>
        /// Typ harmonogramu zadania.
        /// Okreœla jak interpretowaæ pozosta³e pola harmonogramu.
        /// </summary>
        public ScheduleType Type { get; set; } = ScheduleType.Interval;

        /// <summary>
        /// Interwa³ w sekundach dla typu Interval.
        /// Okreœla co ile sekund zadanie ma byæ wykonywane.
        /// Wymagane tylko dla Type = Interval.
        /// </summary>
        /// <example>60 dla wykonywania co minutê</example>
        public int? IntervalSeconds { get; set; }

        /// <summary>
        /// Godzina wykonania dla typów Daily, Weekly, Monthly (0-23).
        /// U¿ywane w po³¹czeniu z Minute do okreœlenia dok³adnego czasu.
        /// </summary>
        /// <example>9 dla godziny 9:00</example>
        public int? Hour { get; set; }

        /// <summary>
        /// Minuta wykonania dla typów Daily, Weekly, Monthly (0-59).
        /// U¿ywane w po³¹czeniu z Hour do okreœlenia dok³adnego czasu.
        /// </summary>
        /// <example>30 dla 30 minut po godzinie</example>
        public int? Minute { get; set; }

        /// <summary>
        /// Dzieñ tygodnia dla typu Weekly (0 = Niedziela, 1 = Poniedzia³ek, itd.).
        /// Wymagane tylko dla Type = Weekly.
        /// </summary>
        /// <example>DayOfWeek.Monday</example>
        public DayOfWeek? DayOfWeek { get; set; }

        /// <summary>
        /// Dzieñ miesi¹ca dla typu Monthly (1-31).
        /// Jeœli miesiêc ma mniej dni, zadanie zostanie wykonane w ostatnim dniu miesi¹ca.
        /// Wymagane tylko dla Type = Monthly.
        /// </summary>
        /// <example>15 dla 15-go dnia miesi¹ca</example>
        public int? DayOfMonth { get; set; }

        /// <summary>
        /// Wyra¿enie cron dla typu Cron.
        /// Format standardowy: "minuta godzina dzieñ-miesi¹ca miesi¹c dzieñ-tygodnia"
        /// Przyk³ad: "0 9 * * 1" = ka¿dy poniedzia³ek o 9:00
        /// UWAGA: Obecnie nie w pe³ni zaimplementowane - placeholder.
        /// </summary>
        /// <example>0 9 * * 1</example>
        public string? CronExpression { get; set; }

        /// <summary>
        /// ¯¹danie HTTP do wykonania gdy zadanie zostanie uruchomione.
        /// Zawiera wszystkie parametry potrzebne do wykonania zadania.
        /// </summary>
        public CronJobRequest Request { get; set; } = new();
    }

    /// <summary>
    /// Typy harmonogramów obs³ugiwane przez scheduler.
    /// Ka¿dy typ ma swoj¹ logikê obliczania nastêpnego czasu wykonania.
    /// </summary>
    public enum ScheduleType
    {
        /// <summary>
        /// Wykonywanie co X sekund.
        /// U¿ywa pola IntervalSeconds.
        /// Przyk³ad: co 60 sekund, co 5 minut (300 sekund).
        /// </summary>
        Interval,

        /// <summary>
        /// Wykonywanie codziennie o okreœlonej godzinie.
        /// U¿ywa pól Hour i Minute.
        /// Przyk³ad: codziennie o 9:30, codziennie o pó³nocy (0:00).
        /// </summary>
        Daily,

        /// <summary>
        /// Wykonywanie co tydzieñ w okreœlony dzieñ o okreœlonej godzinie.
        /// U¿ywa pól DayOfWeek, Hour i Minute.
        /// Przyk³ad: ka¿dy poniedzia³ek o 10:00, ka¿dy pi¹tek o 17:30.
        /// </summary>
        Weekly,

        /// <summary>
        /// Wykonywanie co miesi¹c w okreœlony dzieñ o okreœlonej godzinie.
        /// U¿ywa pól DayOfMonth, Hour i Minute.
        /// Przyk³ad: 1-go dnia miesi¹ca o 8:00, 15-go dnia miesi¹ca o 12:00.
        /// </summary>
        Monthly,

        /// <summary>
        /// Wykonywanie wed³ug wyra¿enia cron.
        /// U¿ywa pola CronExpression.
        /// UWAGA: Obecnie nie w pe³ni zaimplementowane - placeholder dla przysz³ych rozszerzeñ.
        /// </summary>
        Cron
    }

    #region Product Sync Configuration Models - Modele konfiguracji synchronizacji produktów

    /// <summary>
    /// Konfiguracja synchronizacji produktów dla ró¿nych marketplace'ów.
    /// Definiuje parametry ¿¹dañ HTTP i harmonogramu dla zadañ cyklicznych.
    /// </summary>
    public class ProductSyncConfiguration
    {
        /// <summary>
        /// Unikalny identyfikator konfiguracji synchronizacji.
        /// U¿ywany jako JobId w schedulerze zadañ cyklicznych.
        /// </summary>
        /// <example>olmed-sync-products</example>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Nazwa opisowa konfiguracji synchronizacji.
        /// </summary>
        /// <example>Synchronizacja produktów Olmed</example>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Czy konfiguracja jest aktywna.
        /// Nieaktywne konfiguracje nie bêd¹ tworzone jako zadania cykliczne.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Interwa³ wykonywania zadania w sekundach.
        /// </summary>
        /// <example>7200</example>
        public int IntervalSeconds { get; set; } = 7200;

        /// <summary>
        /// Metoda HTTP ¿¹dania.
        /// </summary>
        /// <example>POST</example>
        public string Method { get; set; } = "POST";

        /// <summary>
        /// URL endpointu API do synchronizacji produktów.
        /// </summary>
        /// <example>https://draft-csm-connector.grupaolmed.pl/erp-api/products/get-products</example>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Czy u¿ywaæ automatycznej autoryzacji Olmed.
        /// </summary>
        public bool UseOlmedAuth { get; set; } = true;

        /// <summary>
        /// Nag³ówki HTTP ¿¹dania.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// Zawartoœæ ¿¹dania (body) w formacie JSON string.
        /// </summary>
        /// <example>{"marketplace": "APTEKA_OLMED"}</example>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Marketplace dla którego wykonywana jest synchronizacja.
        /// U¿ywane w body ¿¹dania.
        /// </summary>
        /// <example>APTEKA_OLMED</example>
        public string Marketplace { get; set; } = string.Empty;

        /// <summary>
        /// Dodatkowe parametry specyficzne dla danego marketplace.
        /// </summary>
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();

        /// <summary>
        /// Opis konfiguracji synchronizacji.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kolekcja konfiguracji synchronizacji produktów.
    /// U¿ywana do deserializacji z pliku JSON.
    /// </summary>
    public class ProductSyncConfigurationCollection
    {
        /// <summary>
        /// Lista konfiguracji synchronizacji produktów.
        /// </summary>
        public List<ProductSyncConfiguration> Configurations { get; set; } = new();

        /// <summary>
        /// Wersja konfiguracji - u¿yteczna do migracji.
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Czas ostatniej modyfikacji konfiguracji.
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }

    #endregion
}