namespace Prospeo.DbContext.Enums;

/// <summary>
/// Enum representing the scope of operations in the queue (Queue.Scope)
/// </summary>
public enum QueueScope
{
    /// <summary>
    /// Operation on product
    /// </summary>
    Towar = 16,
    
    /// <summary>
    /// Operation on contractor
    /// </summary>
    Kontrahent = 32,
    
    /// <summary>
    /// Operation on order
    /// </summary>
    Zamowienie = 960,
    
    /// <summary>
    /// Operation on purchase invoice
    /// </summary>
    FakutraZakupu = 1521,
    
    /// <summary>
    /// Correction operation
    /// </summary>
    Korekta = 1529,
    
    /// <summary>
    /// Operation on warehouse stock
    /// </summary>
    Sock = -16,
    
    /// <summary>
    /// Operation on invoice
    /// </summary>
    Faktura = 2033,
    
    /// <summary>
    /// Operation on invoice correction
    /// </summary>
    KorektaFaktury = 2041,
    
    /// <summary>
    /// Operation on RW document (Internal Issue)
    /// </summary>
    RW = 1616,
    
    /// <summary>
    /// Operation on PW document (Internal Receipt)
    /// </summary>
    PW = 1617,
    
    /// <summary>
    /// Operation on MMW document (Inter-warehouse Transfer)
    /// </summary>
    MMW = 1603,

    /// <summary>
    /// Changes Agilero status for order
    /// </summary>
    AgileroStatus = -960,
    /// <summary>
    /// Generates Agilero source inconme document
    /// </summary>
    AgileroIncome = -1521,
    /// <summary>
    /// Generates Agilero source issue document
    /// </summary>
    AgileroRelease = -1616,
   
        
}

/// <summary>
/// Enum representing the status of a task in the queue (Queue.Flg)
/// </summary>
public enum QueueStatusEnum
{
    /// <summary>
    /// Task waiting to be processed
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Task being processed
    /// </summary>
    Processing = 5,

    /// <summary>
    /// Task completed successfully
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Task completed with error
    /// </summary>
    Error = -1
}
