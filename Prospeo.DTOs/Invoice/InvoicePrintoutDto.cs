using Prospeo.DTOs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Prospeo.DTOs.Invoice
{
    public class InvoicePrintoutDto :DTOModelBase
    {
        /// <summary>
        /// Gets or sets the marketplace identifier.
        /// </summary>
        /// <value>
        /// Always set to "OLMED_CSM" for marketing invoices.
        /// </value>
        [JsonPropertyName("marketplace")]
        public string Marketplace { get; set; } = "OLMED_CSM";

        /// <summary>
        /// Gets or sets the GID number associated with the invoice printout.
        /// </summary>
        [JsonPropertyName("gidNumer")]
        public int GidNumer { get; set; }

        /// <summary>
        /// Gets or sets the GID type associated with the invoice printout.
        /// </summary>
        [JsonPropertyName("gidTyp")]
        public int GidTyp { get; set; }
        
        /// <summary>
        /// Gets or sets the endpoint associated with the invoice printout.
        /// </summary>  
        [JsonPropertyName("endpoint")]
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the order number associated with the invoice printout. 
        /// </summary>
        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the invoice number associated with the invoice printout.
        /// </summary>
        [JsonPropertyName("invoiceNumber")]
        public string InvoiceNumber { get; set; }= string.Empty;


    }
}
