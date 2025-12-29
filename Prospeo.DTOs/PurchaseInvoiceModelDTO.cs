using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Represents a single line item on a purchase invoice.
    /// Contains product identification, quantity, pricing, and tax information for invoiced goods.
    /// </summary>
    /// <remarks>
    /// This class captures all essential information about a purchased product line item,
    /// including cost, quantity, and tax details required for accounting and inventory management.
    /// 
    /// <para>
    /// Used primarily for:
    /// <list type="bullet">
    /// <item><description>Recording supplier invoice details</description></item>
    /// <item><description>Updating inventory with purchase costs</description></item>
    /// <item><description>Accounts payable processing</description></item>
    /// <item><description>Tax compliance and VAT reporting</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class InvoiceItem
    {
        #region FIELDS

        private string code;
        private decimal quantity;
        private decimal price;
        private int taxRate;
        private string taxGroup;
        private string feature;
        private int expirationDate;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Gets or sets the product code or SKU.
        /// </summary>
        /// <value>
        /// The unique identifier for the product being purchased (e.g., SKU, product code).
        /// </value>
        /// <remarks>
        /// This code must match the product identifier in the inventory system to enable
        /// automatic inventory updates and cost tracking. Required for all invoice items.
        /// </remarks>
        [Required]
        public string Kod
        {
            get => code;
            set => code = value;
        }

        /// <summary>
        /// Gets or sets the purchased quantity.
        /// </summary>
        /// <value>
        /// The number of units purchased as shown on the supplier's invoice.
        /// </value>
        /// <remarks>
        /// This quantity is used to:
        /// <list type="bullet">
        /// <item><description>Update inventory levels (increase stock)</description></item>
        /// <item><description>Calculate total line item cost</description></item>
        /// <item><description>Update average purchase prices</description></item>
        /// <item><description>Validate goods receipt quantities</description></item>
        /// </list>
        /// Required for all invoice items.
        /// </remarks>
        [Required]
        public decimal Ilosc
        {
            get => quantity;
            set => quantity = value;
        }

        /// <summary>
        /// Gets or sets the net unit price.
        /// </summary>
        /// <value>
        /// The purchase price per unit, excluding VAT/tax.
        /// </value>
        /// <remarks>
        /// This is the net price (before tax) paid to the supplier per unit of the product.
        /// Used for:
        /// <list type="bullet">
        /// <item><description>Calculating total line cost (quantity × price)</description></item>
        /// <item><description>Updating average purchase price in inventory</description></item>
        /// <item><description>Cost accounting and gross margin calculations</description></item>
        /// <item><description>Supplier price analysis and negotiation</description></item>
        /// </list>
        /// Required for all invoice items.
        /// </remarks>
        [Required]
        public decimal Cena
        {
            get => price;
            set => price = value;
        }

        /// <summary>
        /// Gets or sets the VAT rate multiplied by 100.
        /// </summary>
        /// <value>
        /// The VAT percentage multiplied by 100 (e.g., 23% VAT = 2300, 8% VAT = 800).
        /// </value>
        /// <remarks>
        /// This unusual format (rate × 100) is used for compatibility with legacy systems
        /// that store tax rates as integers to avoid floating-point precision issues.
        /// 
        /// <para>
        /// Common Polish VAT rates:
        /// <list type="bullet">
        /// <item><description>2300 = 23% (standard rate)</description></item>
        /// <item><description>800 = 8% (reduced rate for certain goods)</description></item>
        /// <item><description>500 = 5% (reduced rate for specific items)</description></item>
        /// <item><description>0 = 0% (exempt or zero-rated)</description></item>
        /// </list>
        /// </para>
        /// Required for all invoice items for proper tax calculation.
        /// </remarks>
        [Required]
        public int StawkaPod
        {
            get => taxRate;
            set => taxRate = value;
        }

        /// <summary>
        /// Gets or sets the VAT tax group classification.
        /// </summary>
        /// <value>
        /// The tax group code as defined in the accounting system (e.g., "A", "B", "C", "ZW").
        /// </value>
        /// <remarks>
        /// Tax groups categorize products for VAT reporting and compliance purposes.
        /// Common Polish tax groups:
        /// <list type="bullet">
        /// <item><description>"A" - Standard 23% VAT</description></item>
        /// <item><description>"B" - Reduced 8% VAT</description></item>
        /// <item><description>"C" - Reduced 5% VAT</description></item>
        /// <item><description>"ZW" - Exempt (zwolniony)</description></item>
        /// <item><description>"NP" - Not subject to VAT (nie podlega)</description></item>
        /// </list>
        /// 
        /// <para>
        /// This classification is used for:
        /// <list type="bullet">
        /// <item><description>VAT return reporting</description></item>
        /// <item><description>Tax compliance and auditing</description></item>
        /// <item><description>Correct tax calculation on resale</description></item>
        /// </list>
        /// </para>
        /// Required for all invoice items.
        /// </remarks>
        [Required]
        public string Vat
        {
            get => taxGroup;
            set => taxGroup = value;
        }

        #endregion
    }

    /// <summary>
    /// Represents a complete purchase invoice document from a supplier.
    /// Contains header information and a collection of line items for goods or services purchased.
    /// </summary>
    /// <remarks>
    /// This DTO models a supplier invoice (faktura zakupu) and is used to:
    /// <list type="bullet">
    /// <item><description>Register supplier invoices in the accounting system</description></item>
    /// <item><description>Update inventory with purchased goods and their costs</description></item>
    /// <item><description>Track accounts payable and payment due dates</description></item>
    /// <item><description>Maintain audit trail for tax compliance</description></item>
    /// <item><description>Link purchases to specific orders (when applicable)</description></item>
    /// </list>
    /// 
    /// <para>
    /// Inherits from <see cref="DTOModelBase"/> to ensure all string properties
    /// are initialized to empty strings, preventing null reference exceptions.
    /// </para>
    /// </remarks>
    public class PurchaseInvoiceModelDTO : DTOModelBase
    {
        #region FIELDS

        private string sourceNumber;
        private int documentType;
        private string shortCode;
        private int paymentTerm;
        private ICollection<InvoiceItem> items = new HashSet<InvoiceItem>();
        private string description;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Gets or sets the supplier's original invoice number.
        /// </summary>
        /// <value>
        /// The invoice number as it appears on the supplier's document (e.g., "FV/2024/01/0156").
        /// </value>
        /// <remarks>
        /// This is the external document number from the supplier, not the internal document number
        /// in your accounting system. It's essential for:
        /// <list type="bullet">
        /// <item><description>Matching payments to invoices</description></item>
        /// <item><description>Communication with suppliers</description></item>
        /// <item><description>Resolving discrepancies</description></item>
        /// <item><description>Tax audits and compliance verification</description></item>
        /// </list>
        /// Required field.
        /// </remarks>
        [Required]
        public string DokumentObcy
        {
            get => sourceNumber;
            set => sourceNumber = value;
        }

        /// <summary>
        /// Gets or sets the payment due date in Clarion date format.
        /// </summary>
        /// <value>
        /// The payment deadline represented as a Clarion date (integer days since a base date).
        /// </value>
        /// <remarks>
        /// Clarion date format is a legacy format where dates are stored as the number of days
        /// since December 28, 1800. This format is still used in some Polish ERP systems.
        /// 
        /// <para>
        /// The payment term determines:
        /// <list type="bullet">
        /// <item><description>When payment is due to the supplier</description></item>
        /// <item><description>Cash flow planning and working capital management</description></item>
        /// <item><description>Early payment discount eligibility</description></item>
        /// <item><description>Late payment penalty calculations</description></item>
        /// </list>
        /// </para>
        /// Required field.
        /// </remarks>
        [Required]
        public int Termin
        {
            get => paymentTerm;
            set => paymentTerm = value;
        }

        /// <summary>
        /// Gets or sets the document type code according to the XL ERP system.
        /// </summary>
        /// <value>
        /// A numeric code identifying the type of purchase document in the XL system.
        /// </value>
        /// <remarks>
        /// Document types in XL systems typically include:
        /// <list type="bullet">
        /// <item><description>Purchase invoices (faktury zakupu)</description></item>
        /// <item><description>Advance invoices (faktury zaliczkowe)</description></item>
        /// <item><description>Correction invoices (faktury korygujące)</description></item>
        /// <item><description>Pro forma invoices</description></item>
        /// </list>
        /// 
        /// <para>
        /// The document type affects:
        /// <list type="bullet">
        /// <item><description>Accounting entries generated</description></item>
        /// <item><description>VAT reporting treatment</description></item>
        /// <item><description>Inventory impact</description></item>
        /// <item><description>Document numbering series</description></item>
        /// </list>
        /// </para>
        /// Required field.
        /// </remarks>
        [Required]
        public int Typ
        {
            get => documentType;
            set => documentType = value;
        }

        /// <summary>
        /// Gets or sets the supplier's acronym or short code.
        /// </summary>
        /// <value>
        /// A short identifier or code for the supplier (e.g., "SUPP001", "ABC-DIST").
        /// </value>
        /// <remarks>
        /// This code identifies the supplier/vendor in the system and is used to:
        /// <list type="bullet">
        /// <item><description>Link the invoice to the correct supplier account</description></item>
        /// <item><description>Update accounts payable for the specific vendor</description></item>
        /// <item><description>Generate supplier payment reports</description></item>
        /// <item><description>Track purchase history by supplier</description></item>
        /// </list>
        /// 
        /// <para>
        /// Must correspond to an existing supplier record in the system.
        /// </para>
        /// Required field.
        /// </remarks>
        [Required]
        public string Akronim
        {
            get => shortCode;
            set => shortCode = value;
        }

        /// <summary>
        /// Gets or sets an optional description or notes about the invoice.
        /// </summary>
        /// <value>
        /// Free-text description or comments about the invoice document.
        /// </value>
        /// <remarks>
        /// This field can contain:
        /// <list type="bullet">
        /// <item><description>Special terms or conditions</description></item>
        /// <item><description>Delivery information</description></item>
        /// <item><description>Quality notes</description></item>
        /// <item><description>References to related documents (purchase orders, delivery notes)</description></item>
        /// <item><description>Any other relevant contextual information</description></item>
        /// </list>
        /// Optional field.
        /// </remarks>
        public string Opis
        {
            get => description;
            set => description = value;
        }

        /// <summary>
        /// Gets or sets the order number associated with this purchase invoice (if applicable).
        /// </summary>
        /// <value>
        /// The reference number of the purchase order that this invoice relates to.
        /// Returns <see cref="string.Empty"/> if the invoice is not linked to a specific order.
        /// </value>
        /// <remarks>
        /// Linking invoices to orders enables:
        /// <list type="bullet">
        /// <item><description>Three-way matching (PO - Goods Receipt - Invoice)</description></item>
        /// <item><description>Automatic validation of quantities and prices</description></item>
        /// <item><description>Tracking order completion status</description></item>
        /// <item><description>Preventing duplicate payments</description></item>
        /// <item><description>Discrepancy detection and resolution</description></item>
        /// </list>
        /// </remarks>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of line items on this invoice.
        /// </summary>
        /// <value>
        /// A collection of <see cref="InvoiceItem"/> objects representing individual products or services invoiced.
        /// </value>
        /// <remarks>
        /// Each item in the collection represents a line on the invoice, containing:
        /// <list type="bullet">
        /// <item><description>Product identification (SKU/code)</description></item>
        /// <item><description>Quantity purchased</description></item>
        /// <item><description>Unit price</description></item>
        /// <item><description>Tax information</description></item>
        /// </list>
        /// 
        /// <para>
        /// The collection is used to:
        /// <list type="bullet">
        /// <item><description>Update inventory levels for each product</description></item>
        /// <item><description>Calculate total invoice value</description></item>
        /// <item><description>Generate detailed accounting entries</description></item>
        /// <item><description>Produce VAT reports by product category</description></item>
        /// </list>
        /// </para>
        /// 
        /// <para>
        /// Required field - an invoice must have at least one item.
        /// </para>
        /// </remarks>
        [Required]
        public ICollection<InvoiceItem> Items
        {
            get => items;
            set => items = value;
        }

        #endregion
    }
}
