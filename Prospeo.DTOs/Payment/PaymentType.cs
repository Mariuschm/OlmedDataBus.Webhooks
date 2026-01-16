namespace Prospeo.DTOs.Payment
{
    /// <summary>
    /// Represents a payment type from the system configuration.
    /// </summary>
    public class PaymentType
    {
        /// <summary>
        /// Gets or sets the line position (Lp) identifier of the payment type.
        /// </summary>
        public int Lp { get; set; }

        /// <summary>
        /// Gets or sets the name of the payment type.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
