using Prospeo.DTOs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prospeo.DTOs.Invoice
{
    /// <summary>
    /// Represents a sales invoice for Zawisza - differs from one from Olmed
    /// </summary>
    /// <remarks>
    /// This class is used to transfer sales invoice data between different layers of the application.
    /// It inherits from <see cref="DTOModelBase"/>, which provides automatic initialization of string
    /// properties to prevent null reference exceptions.
    /// 
    /// <para>
    /// The sales invoice contains a reference to an order and a collection of invoice items that
    /// represent the products or services being invoiced.
    /// </para>
    /// </remarks>
    public class SalesInvoiceDTO : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the unique identifier of the order associated with this sales invoice.
        /// </summary>
        /// <value>
        /// The order identifier that links this invoice to its corresponding order.
        /// </value>
        public int orderId { get; set; }

        /// <summary>
        /// Gets or sets the collection of line items included in this sales invoice.
        /// </summary>
        /// <value>
        /// A list of <see cref="SalesInvoiceItemDTO"/> objects representing the individual items on the invoice.
        /// Defaults to an empty list to prevent null reference exceptions.
        /// </value>
        public List<SalesInvoiceItemDTO> items { get; set; } = new List<SalesInvoiceItemDTO>();
        /// <summary>
        /// Gets or sets the unique identifier of the invoice in the external system.
        /// </summary>
        /// <value>
        /// The invoice identifier that uniquely identifies this invoice in the target system.
        /// This value is typically assigned by the external system after the invoice is created.
        /// </value>
        public int invoiceId { get; set; }
    }

    /// <summary>
    /// Represents a single line item on a sales invoice.
    /// </summary>
    /// <remarks>
    /// This class captures the details of an individual article or product being invoiced,
    /// including quantity and references to the original order. It inherits from <see cref="DTOModelBase"/>
    /// to benefit from automatic string property initialization.
    /// </remarks>
    public class SalesInvoiceItemDTO : DTOModelBase
    {
        /// <summary>
        /// Gets or sets the unique identifier of the article being invoiced.
        /// </summary>
        /// <value>
        /// The article identifier that references the product or service in the system.
        /// </value>
        public string articleCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity of the article being invoiced.
        /// </summary>
        /// <value>
        /// The quantity as a decimal value to support fractional quantities (e.g., 2.5 units).
        /// </value>
        public decimal quantity { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the order associated with this invoice item.
        /// </summary>
        /// <value>
        /// The order identifier that links this item to its corresponding order.
        /// </value>
        public int orderId { get; set; }

        /// <summary>
        /// Gets or sets the line number of this item within the original order.
        /// </summary>
        /// <value>
        /// The line number used to maintain the order of items and reference the original order line.
        /// </value>
        public int orderLineNo { get; set; }
    }
}
