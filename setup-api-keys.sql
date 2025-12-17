-- ========================================
-- SQL Script: Konfiguracja API Keys dla kontrolerów Order i Invoice
-- ========================================
-- Ten skrypt dodaje przyk³adowe klucze API do tabeli Firmy
-- Umo¿liwia to autoryzacjê ¿¹dañ do nowych kontrolerów

-- ========================================
-- 1. Sprawdzenie istniej¹cych firm
-- ========================================
SELECT 
    Id,
    NazwaFirmy,
    NazwaBazyERP,
    CzyTestowa,
    ApiKey,
    AuthorizeAllEndpoints
FROM ProRWS.Firmy
ORDER BY Id;

-- ========================================
-- 2. Dodanie API Key do istniej¹cej firmy
-- ========================================
-- Przyk³ad: Dodanie API Key do firmy o ID = 1002
-- UWAGA: Zmieñ wartoœæ ApiKey na unikalny, bezpieczny klucz!

UPDATE ProRWS.Firmy
SET 
    ApiKey = 'YOUR_SECRET_API_KEY_HERE_MIN_32_CHARS',  -- Zmieñ na w³asny, bezpieczny klucz
    AuthorizeAllEndpoints = 1  -- Opcjonalne: czy autoryzowaæ wszystkie endpointy
WHERE Id = 1002;

-- Weryfikacja:
SELECT 
    Id,
    NazwaFirmy,
    ApiKey,
    AuthorizeAllEndpoints
FROM ProRWS.Firmy
WHERE Id = 1002;

-- ========================================
-- 3. Dodanie nowej firmy z API Key
-- ========================================
-- Jeœli chcesz dodaæ now¹ firmê z kluczem API:

-- INSERT INTO ProRWS.Firmy (NazwaFirmy, NazwaBazyERP, CzyTestowa, ApiKey, AuthorizeAllEndpoints)
-- VALUES (
--     'Przyk³adowa Firma API',
--     'TESTDB',
--     1,  -- CzyTestowa = 1 (firma testowa)
--     'YOUR_SECRET_API_KEY_HERE_MIN_32_CHARS',  -- API Key (32+ znaków)
--     1   -- AuthorizeAllEndpoints = 1
-- );

-- ========================================
-- 4. Generowanie bezpiecznego API Key
-- ========================================
-- W SQL Server mo¿na wygenerowaæ losowy API Key:

DECLARE @ApiKey NVARCHAR(100);
SET @ApiKey = 'apikey_' + REPLACE(CAST(NEWID() AS NVARCHAR(36)), '-', '') + REPLACE(CAST(NEWID() AS NVARCHAR(36)), '-', '');

SELECT @ApiKey AS GeneratedApiKey;

-- Kopiuj wygenerowany klucz i u¿yj go w UPDATE lub INSERT

-- ========================================
-- 5. Sprawdzenie wszystkich firm z API Keys
-- ========================================
SELECT 
    Id,
    NazwaFirmy,
    ApiKey,
    CASE 
        WHEN ApiKey IS NULL THEN 'Brak klucza'
        ELSE 'Klucz ustawiony'
    END AS ApiKeyStatus,
    AuthorizeAllEndpoints
FROM ProRWS.Firmy
WHERE ApiKey IS NOT NULL
ORDER BY Id;

-- ========================================
-- 6. Usuniêcie API Key (jeœli potrzebne)
-- ========================================
-- UPDATE ProRWS.Firmy
-- SET ApiKey = NULL, AuthorizeAllEndpoints = NULL
-- WHERE Id = 1002;

-- ========================================
-- 7. Testowanie API Key
-- ========================================
-- Po ustawieniu API Key, przetestuj go u¿ywaj¹c:
-- PowerShell: .\test-order-invoice-api.ps1
-- cURL: curl -H "X-API-Key: twój-klucz" http://localhost:5000/api/order/authenticated-firma

-- ========================================
-- UWAGI BEZPIECZEÑSTWA
-- ========================================
-- 1. API Keys powinny byæ d³ugie (minimum 32 znaki) i losowe
-- 2. U¿ywaj opisowego prefiksu: 'apikey_live_' dla produkcji, 'apikey_test_' dla testów
-- 3. Nigdy nie udostêpniaj API Keys publicznie (repozytoria Git, logi, etc.)
-- 4. Regularnie rotuj API Keys (np. co 3-6 miesiêcy)
-- 5. U¿ywaj ró¿nych kluczy dla ró¿nych firm/partnerów
-- 6. Przechowuj kopiê zapasow¹ kluczy w bezpiecznym miejscu
-- 7. Monitoruj u¿ycie API Keys w logach aplikacji

-- ========================================
-- PRZYK£ADOWE FORMATY API KEYS
-- ========================================
-- Produkcja: apikey_live_1a2b3c4d5e6f7g8h9i0j1k2l3m4n5o6p7q8r9s0t
-- Test:      apikey_test_9z8y7x6w5v4u3t2s1r0q9p8o7n6m5l4k3j2i1h0g
-- Partner:   apikey_partner_companyname_a1b2c3d4e5f6g7h8i9j0

-- ========================================
-- PODSUMOWANIE
-- ========================================
-- Po wykonaniu tego skryptu:
-- 1. SprawdŸ czy API Key zosta³ dodany do tabeli Firmy
-- 2. Zapisz API Key w bezpiecznym miejscu
-- 3. U¿yj API Key w nag³ówku X-API-Key przy wysy³aniu ¿¹dañ
-- 4. Przetestuj autoryzacjê u¿ywaj¹c skryptu test-order-invoice-api.ps1
