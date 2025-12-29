using Prospeo.DTOs.Core;
using System.Text.Json.Serialization;

namespace Prospeo.DTOs.Order
{
    /// <summary>
    /// Represents additional marketplace-specific data associated with an order.
    /// This DTO contains supplementary information that varies depending on the marketplace platform.
    /// </summary>
    /// <remarks>
    /// This class captures marketplace-specific identifiers and payment information that may be
    /// required for order processing, tracking, or reconciliation across different e-commerce platforms.
    /// 
    /// <para>
    /// Common use cases include:
    /// <list type="bullet">
    /// <item><description>Linking orders to marketplace platform identifiers (e.g., Allegro order ID)</description></item>
    /// <item><description>Tracking payment provider information for financial reconciliation</description></item>
    /// <item><description>Storing marketplace-specific transaction identifiers</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OrderMarketplaceAdditionalDataDto : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the Allegro order identifier.
        /// </summary>
        /// <value>
        /// The unique order identifier from the Allegro marketplace platform.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// This property is used when the order originates from the Allegro marketplace
        /// and needs to be tracked or referenced back to the original platform.
        /// </remarks>
        [JsonPropertyName("allegroOrderId")]
        [SpecialProperty("Atrybut")]
        public string AllegroOrderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the payment provider name or identifier.
        /// </summary>
        /// <value>
        /// The name or identifier of the payment service provider (e.g., "PayU", "Przelewy24", "Stripe").
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// This information is useful for payment reconciliation, financial reporting,
        /// and troubleshooting payment-related issues.
        /// </remarks>
        [JsonPropertyName("paymentProvider")]
        [SpecialProperty("Atrybut")]
        public string PaymentProvider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the payment transaction identifier.
        /// </summary>
        /// <value>
        /// The unique payment transaction ID from the payment provider.
        /// Returns <see cref="string.Empty"/> if not applicable or not provided.
        /// </value>
        /// <remarks>
        /// This ID can be used to track payment status, perform refunds, or investigate
        /// payment-related issues with the payment provider.
        /// </remarks>
        [JsonPropertyName("paymentId")]
        [SpecialProperty("Atrybut")]
        public string PaymentId { get; set; } = string.Empty;
    }
}
