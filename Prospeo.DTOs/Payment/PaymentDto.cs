using Prospeo.DTOs.Core;
using System.ComponentModel.DataAnnotations;

namespace Prospeo.DTOs.Payment
{
    /// <summary>
    /// Represents a payment transaction in the system.
    /// Contains information about payment method, amount, date, and related documents.
    /// </summary>
    /// <remarks>
    /// This DTO models payment transactions and is used for:
    /// <list type="bullet">
    /// <item><description>Recording customer payments</description></item>
    /// <item><description>Bank and cash register operations</description></item>
    /// <item><description>Payment reconciliation</description></item>
    /// <item><description>Financial reporting</description></item>
    /// </list>
    /// </remarks>
    public class PaymentDto : DTOModelBase
    {
        private string documentNumber;
        private int paymentFormId;
        private decimal amount;
        private int paymentDate;
        private string contractorCode;
        private string description;
        private string currency = "PLN";

        /// <summary>
        /// Gets or sets the payment document number.
        /// </summary>
        /// <value>
        /// The unique document number for this payment transaction.
        /// </value>
        /// <remarks>
        /// This is typically an internally generated number from the payment register.
        /// Required field.
        /// </remarks>
        [Required]
        public string DocumentNumber
        {
            get => documentNumber;
            set => documentNumber = value;
        }

        /// <summary>
        /// Gets or sets the payment form identifier.
        /// </summary>
        /// <value>
        /// The ID of the payment method (cash, bank transfer, card, etc.).
        /// </value>
        /// <remarks>
        /// References the FormaPlatnosci (payment form) in the XL system.
        /// Common values:
        /// <list type="bullet">
        /// <item><description>Cash payment</description></item>
        /// <item><description>Bank transfer</description></item>
        /// <item><description>Credit card</description></item>
        /// <item><description>Check</description></item>
        /// </list>
        /// Required field.
        /// </remarks>
        [Required]
        public int PaymentFormId
        {
            get => paymentFormId;
            set => paymentFormId = value;
        }

        /// <summary>
        /// Gets or sets the payment amount.
        /// </summary>
        /// <value>
        /// The payment amount in the specified currency.
        /// </value>
        /// <remarks>
        /// Should be positive for receipts, negative for disbursements.
        /// Required field.
        /// </remarks>
        [Required]
        public decimal Amount
        {
            get => amount;
            set => amount = value;
        }

        /// <summary>
        /// Gets or sets the payment date in Clarion date format.
        /// </summary>
        /// <value>
        /// The date when the payment was made (Clarion date format).
        /// </value>
        /// <remarks>
        /// Clarion date format: days since December 28, 1800.
        /// Required field.
        /// </remarks>
        [Required]
        public int PaymentDate
        {
            get => paymentDate;
            set => paymentDate = value;
        }

        /// <summary>
        /// Gets or sets the contractor code.
        /// </summary>
        /// <value>
        /// The code/acronym of the contractor (payer/payee).
        /// </value>
        /// <remarks>
        /// References KntKarty.Knt_Akronim in the system.
        /// Required field.
        /// </remarks>
        [Required]
        public string ContractorCode
        {
            get => contractorCode;
            set => contractorCode = value;
        }

        /// <summary>
        /// Gets or sets the payment description.
        /// </summary>
        /// <value>
        /// Free-text description of the payment purpose.
        /// </value>
        /// <remarks>
        /// Optional field for additional payment details.
        /// </remarks>
        public string Description
        {
            get => description;
            set => description = value;
        }

        /// <summary>
        /// Gets or sets the currency code.
        /// </summary>
        /// <value>
        /// Three-letter ISO currency code (default: PLN).
        /// </value>
        /// <remarks>
        /// Optional field, defaults to PLN (Polish Zloty).
        /// </remarks>
        public string Currency
        {
            get => currency;
            set => currency = value;
        }

        /// <summary>
        /// Gets or sets the related document GID (optional).
        /// </summary>
        /// <value>
        /// The GIDNumer of the related document (invoice, order, etc.).
        /// </value>
        /// <remarks>
        /// Optional - used to link payment to specific documents.
        /// </remarks>
        public int? RelatedDocumentId { get; set; }

        /// <summary>
        /// Gets or sets the related document type (optional).
        /// </summary>
        /// <value>
        /// The GIDTyp of the related document.
        /// </value>
        /// <remarks>
        /// Optional - specifies the type of related document.
        /// </remarks>
        public int? RelatedDocumentType { get; set; }

        /// <summary>
        /// Gets or sets the bank account number (for bank transfers).
        /// </summary>
        /// <value>
        /// The bank account number used for the transaction.
        /// </value>
        /// <remarks>
        /// Optional - required for bank transfer payments.
        /// </remarks>
        public string? BankAccountNumber { get; set; }
    }
}
