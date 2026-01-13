namespace Prospeo.DbContext.Enums;

/// <summary>
/// Enum reprezentuj¹cy zakres operacji w kolejce (Queue.Scope)
/// </summary>
public enum QueueScope
{
    /// <summary>
    /// Operacja na towarze
    /// </summary>
    Towar = 16,
    Kontrahent = 32,
    Zamowienie = 960,
    FakutraZakupu = 1521,
    Korekta = 1529,
    Sock = -16
}

/// <summary>
/// Enum reprezentuj¹cy status zadania w kolejce (Queue.Flg)
/// </summary>
public enum QueueStatusEnum
{
    /// <summary>
    /// Zadanie oczekuje na przetworzenie
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Zadanie w trakcie przetwarzania
    /// </summary>
    Processing = 5,
    
    /// <summary>
    /// Zadanie zakoñczone pomyœlnie
    /// </summary>
    Completed = 1,
    
    /// <summary>
    /// Zadanie zakoñczone z b³êdem
    /// </summary>
    Error = -1
}
