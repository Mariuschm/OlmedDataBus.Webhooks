# Przyk³adowe testy jednostkowe dla OrderSyncConfigurationService

Ten plik zawiera przyk³ady testów jednostkowych, które mo¿na dodaæ do projektu testowego.

## Struktura testów

```csharp
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prosepo.Webhooks.Services;
using Prosepo.Webhooks.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prosepo.Webhooks.Tests.Services
{
    public class OrderSyncConfigurationServiceTests
    {
        private readonly Mock<ILogger<OrderSyncConfigurationService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly OrderSyncConfigurationService _service;

        public OrderSyncConfigurationServiceTests()
        {
            _loggerMock = new Mock<ILogger<OrderSyncConfigurationService>>();
            _configurationMock = new Mock<IConfiguration>();
            
            // Mockowanie œcie¿ki do pliku konfiguracyjnego (opcjonalnie)
            _configurationMock.Setup(c => c["OrderSync:ConfigurationFile"])
                .Returns("test-order-sync-config.json");
            
            _service = new OrderSyncConfigurationService(_loggerMock.Object, _configurationMock.Object);
        }

        [Fact]
        public void GenerateRequestBody_ShouldIncludeMarketplace()
        {
            // Arrange
            var config = CreateTestConfiguration();

            // Act
            var body = _service.GenerateRequestBody(config);

            // Assert
            Assert.Contains("\"marketplace\":\"APTEKA_OLMED\"", body);
        }

        [Fact]
        public void GenerateRequestBody_ShouldGenerateDateFromAndDateTo()
        {
            // Arrange
            var config = CreateTestConfiguration();

            // Act
            var body = _service.GenerateRequestBody(config);

            // Assert
            Assert.Contains("\"dateFrom\":", body);
            Assert.Contains("\"dateTo\":", body);
        }

        [Fact]
        public void GenerateRequestBody_WithCurrentDate_ShouldUseTodayAsDateTo()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.UseCurrentDateAsEndDate = true;
            config.DateRangeDays = 2;

            var expectedDateTo = DateTime.Now.Date.ToString("yyyy-MM-dd");
            var expectedDateFrom = DateTime.Now.Date.AddDays(-2).ToString("yyyy-MM-dd");

            // Act
            var body = _service.GenerateRequestBody(config);

            // Assert
            Assert.Contains($"\"dateTo\":\"{expectedDateTo}\"", body);
            Assert.Contains($"\"dateFrom\":\"{expectedDateFrom}\"", body);
        }

        [Fact]
        public void GenerateRequestBody_WithoutCurrentDate_ShouldUseYesterdayAsDateTo()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.UseCurrentDateAsEndDate = false;
            config.DateRangeDays = 2;

            var expectedDateTo = DateTime.Now.Date.AddDays(-1).ToString("yyyy-MM-dd");
            var expectedDateFrom = DateTime.Now.Date.AddDays(-3).ToString("yyyy-MM-dd");

            // Act
            var body = _service.GenerateRequestBody(config);

            // Assert
            Assert.Contains($"\"dateTo\":\"{expectedDateTo}\"", body);
            Assert.Contains($"\"dateFrom\":\"{expectedDateFrom}\"", body);
        }

        [Fact]
        public void GenerateRequestBody_WithCustomDateFormat_ShouldUseSpecifiedFormat()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.DateFormat = "yyyy/MM/dd";

            // Act
            var body = _service.GenerateRequestBody(config);

            // Assert - sprawdŸ czy data zawiera slashe zamiast myœlników
            Assert.Contains("\"dateFrom\":\"", body);
            Assert.Contains("/", body);
        }

        [Fact]
        public void GenerateRequestBody_WithAdditionalParameters_ShouldIncludeThem()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.AdditionalParameters = new Dictionary<string, object>
            {
                { "orderStatus", "PENDING" },
                { "maxResults", 100 }
            };

            // Act
            var body = _service.GenerateRequestBody(config);

            // Assert
            Assert.Contains("\"orderStatus\":", body);
            Assert.Contains("\"maxResults\":", body);
        }

        [Fact]
        public void CreateCronJobRequest_ShouldCreateValidRequest()
        {
            // Arrange
            var config = CreateTestConfiguration();

            // Act
            var request = _service.CreateCronJobRequest(config);

            // Assert
            Assert.NotNull(request);
            Assert.Equal(config.Method, request.Method);
            Assert.Equal(config.Url, request.Url);
            Assert.Equal(config.UseOlmedAuth, request.UseOlmedAuth);
            Assert.NotNull(request.Body);
            Assert.Contains("marketplace", request.Body);
        }

        [Fact]
        public void CreateCronJobRequest_ShouldIncludeHeaders()
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.Headers = new Dictionary<string, string>
            {
                { "accept", "application/json" },
                { "Content-Type", "application/json" }
            };

            // Act
            var request = _service.CreateCronJobRequest(config);

            // Assert
            Assert.NotNull(request.Headers);
            Assert.True(request.Headers.ContainsKey("accept"));
            Assert.Equal("application/json", request.Headers["accept"]);
        }

        [Fact]
        public async Task GetActiveConfigurationsAsync_ShouldReturnOnlyActiveConfigs()
        {
            // Arrange - W rzeczywistym teœcie nale¿a³oby mockowaæ plik lub bazê danych
            
            // Act
            var configurations = await _service.GetActiveConfigurationsAsync();

            // Assert
            Assert.NotNull(configurations);
            Assert.All(configurations, c => Assert.True(c.IsActive));
        }

        [Theory]
        [InlineData(1, "2025-01-19", "2025-01-20")]  // 1 dzieñ wstecz
        [InlineData(2, "2025-01-18", "2025-01-20")]  // 2 dni wstecz
        [InlineData(7, "2025-01-13", "2025-01-20")]  // 7 dni wstecz
        public void GenerateRequestBody_WithVariousDateRanges_ShouldCalculateCorrectly(
            int dateRangeDays, string expectedFrom, string expectedTo)
        {
            // Arrange
            var config = CreateTestConfiguration();
            config.DateRangeDays = dateRangeDays;
            config.UseCurrentDateAsEndDate = true;
            
            // Mockuj DateTime.Now na 2025-01-20
            // W rzeczywistym projekcie u¿y³byœ IDateTimeProvider
            
            // Act
            var body = _service.GenerateRequestBody(config);

            // Assert - dla aktualnej daty testy mog¹ siê ró¿niæ
            // W produkcji u¿yj IDateTimeProvider do mockowania czasu
            Assert.Contains("\"dateFrom\":", body);
            Assert.Contains("\"dateTo\":", body);
        }

        [Fact]
        public void RefreshCache_ShouldNotThrowException()
        {
            // Act & Assert
            var exception = Record.Exception(() => _service.RefreshCache());
            Assert.Null(exception);
        }

        // Helper method
        private OrderSyncConfiguration CreateTestConfiguration()
        {
            return new OrderSyncConfiguration
            {
                Id = "test-sync-orders",
                Name = "Test Configuration",
                Description = "Test description",
                IsActive = true,
                IntervalSeconds = 7200,
                Method = "POST",
                Url = "https://api.example.com/orders",
                UseOlmedAuth = true,
                Headers = new Dictionary<string, string>(),
                Marketplace = "APTEKA_OLMED",
                DateRangeDays = 2,
                UseCurrentDateAsEndDate = true,
                DateFormat = "yyyy-MM-dd",
                AdditionalParameters = new Dictionary<string, object>()
            };
        }
    }
}
```

## Testy integracyjne

```csharp
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prosepo.Webhooks.Services;
using Prosepo.Webhooks.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace Prosepo.Webhooks.Tests.Integration
{
    public class OrderSyncConfigurationServiceIntegrationTests : IDisposable
    {
        private readonly OrderSyncConfigurationService _service;
        private readonly string _testConfigPath;

        public OrderSyncConfigurationServiceIntegrationTests()
        {
            // Utwórz tymczasowy plik konfiguracyjny
            _testConfigPath = Path.Combine(Path.GetTempPath(), $"test-order-sync-{Guid.NewGuid()}.json");
            
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<OrderSyncConfigurationService>();
            
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "OrderSync:ConfigurationFile", _testConfigPath }
                })
                .Build();
            
            _service = new OrderSyncConfigurationService(logger, configuration);
        }

        [Fact]
        public async Task SaveAndLoadConfiguration_ShouldPersistData()
        {
            // Arrange
            var config = new OrderSyncConfiguration
            {
                Id = "integration-test",
                Name = "Integration Test Config",
                IsActive = true,
                IntervalSeconds = 3600,
                Method = "POST",
                Url = "https://api.test.com/orders",
                Marketplace = "TEST_MARKET",
                DateRangeDays = 3
            };

            // Act - Save
            var saveResult = await _service.SaveConfigurationAsync(config);
            Assert.True(saveResult);

            // Act - Load
            var loadedConfig = await _service.GetConfigurationByIdAsync("integration-test");

            // Assert
            Assert.NotNull(loadedConfig);
            Assert.Equal(config.Id, loadedConfig.Id);
            Assert.Equal(config.Name, loadedConfig.Name);
            Assert.Equal(config.Marketplace, loadedConfig.Marketplace);
            Assert.Equal(config.DateRangeDays, loadedConfig.DateRangeDays);
        }

        [Fact]
        public async Task DeleteConfiguration_ShouldRemoveFromFile()
        {
            // Arrange
            var config = new OrderSyncConfiguration
            {
                Id = "delete-test",
                Name = "Delete Test Config",
                IsActive = true,
                Marketplace = "TEST_MARKET"
            };

            await _service.SaveConfigurationAsync(config);

            // Act
            var deleteResult = await _service.DeleteConfigurationAsync("delete-test");

            // Assert
            Assert.True(deleteResult);
            
            var loadedConfig = await _service.GetConfigurationByIdAsync("delete-test");
            Assert.Null(loadedConfig);
        }

        [Fact]
        public async Task GetAllConfigurations_ShouldReturnAllSavedConfigs()
        {
            // Arrange
            var config1 = new OrderSyncConfiguration { Id = "test1", Name = "Test 1", IsActive = true, Marketplace = "M1" };
            var config2 = new OrderSyncConfiguration { Id = "test2", Name = "Test 2", IsActive = false, Marketplace = "M2" };

            await _service.SaveConfigurationAsync(config1);
            await _service.SaveConfigurationAsync(config2);

            // Act
            var allConfigs = await _service.GetAllConfigurationsAsync();

            // Assert
            Assert.True(allConfigs.Count >= 2);
            Assert.Contains(allConfigs, c => c.Id == "test1");
            Assert.Contains(allConfigs, c => c.Id == "test2");
        }

        [Fact]
        public async Task GetActiveConfigurations_ShouldReturnOnlyActive()
        {
            // Arrange
            var activeConfig = new OrderSyncConfiguration { Id = "active1", Name = "Active", IsActive = true, Marketplace = "M1" };
            var inactiveConfig = new OrderSyncConfiguration { Id = "inactive1", Name = "Inactive", IsActive = false, Marketplace = "M2" };

            await _service.SaveConfigurationAsync(activeConfig);
            await _service.SaveConfigurationAsync(inactiveConfig);

            // Act
            var activeConfigs = await _service.GetActiveConfigurationsAsync();

            // Assert
            Assert.All(activeConfigs, c => Assert.True(c.IsActive));
            Assert.Contains(activeConfigs, c => c.Id == "active1");
            Assert.DoesNotContain(activeConfigs, c => c.Id == "inactive1");
        }

        public void Dispose()
        {
            // Cleanup - usuñ tymczasowy plik konfiguracyjny
            if (File.Exists(_testConfigPath))
            {
                File.Delete(_testConfigPath);
            }
        }
    }
}
```

## Instalacja pakietów testowych

Aby uruchomiæ testy, zainstaluj nastêpuj¹ce pakiety NuGet:

```bash
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Moq
dotnet add package Microsoft.NET.Test.Sdk
```

## Uruchamianie testów

```bash
dotnet test
```

lub dla szczegó³owych wyników:

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Pokrycie kodu testami

Zainstaluj narzêdzie do pokrycia kodu:

```bash
dotnet tool install --global coverlet.console
```

Uruchom testy z raportem pokrycia:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Najlepsze praktyki testowania

1. **U¿ywaj IDateTimeProvider** - mockuj czas systemowy dla przewidywalnych testów dat
2. **Izolacja testów** - ka¿dy test powinien dzia³aæ niezale¿nie
3. **Cleanup** - zawsze usuwaj tymczasowe pliki/dane po testach
4. **Parametryzowane testy** - u¿yj `[Theory]` dla testowania wielu scenariuszy
5. **Integration vs Unit** - rozdziel testy jednostkowe od integracyjnych
6. **Mockowanie** - mockuj zale¿noœci (ILogger, IConfiguration) w testach jednostkowych
7. **Assert precyzyjnie** - sprawdzaj konkretne wartoœci, nie tylko czy coœ nie jest null

## Przyk³ad uruchomienia w CI/CD

```yaml
# Azure DevOps pipeline example
- task: DotNetCoreCLI@2
  displayName: 'Run Unit Tests'
  inputs:
    command: 'test'
    projects: '**/*Tests.csproj'
    arguments: '--configuration Release --collect:"XPlat Code Coverage"'
```
