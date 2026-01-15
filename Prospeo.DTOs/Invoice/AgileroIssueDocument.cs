using Prospeo.DTOs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prospeo.DTOs.Invoice
{
    /// <summary>
    /// Represents an Agilero Issue Document (WZ - document of warehouse release)
    /// Contains information about items issued from warehouse with series numbers and expiration dates
    /// </summary>
    public class AgileroIssueDocument : DTOModelBase
    {
        /// <summary>
        /// Master system ID - order number in the main system
        /// </summary>
        public int MasterSystemId { get; set; }

        /// <summary>
        /// Tracking number for shipment
        /// </summary>
        public string TrackingNumber { get; set; } = string.Empty;

        /// <summary>
        /// Document status: 0 = Pending, 1 = Done
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Document type
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Creator/operator who created the document
        /// </summary>
        public string Creator { get; set; } = string.Empty;

        /// <summary>
        /// List of items in the issue document
        /// </summary>
        public List<AgileroIssueDocumentItem> Items { get; set; } = new List<AgileroIssueDocumentItem>();

        /// <summary>
        /// Result of allocation validation and assignment of release items to order lines.
        /// Contains allocated items per order line with complete validation status.
        /// This property is populated during processing and contains the mapping between
        /// order lines and their corresponding release items with series numbers and expiration dates.
        /// </summary>
        /// <remarks>
        /// This property is set by the allocation process and contains:
        /// - List of allocations for each order line
        /// - Validation errors (shortages, overages)
        /// - Warnings about unallocated items
        /// - Complete audit trail with series numbers and quantities
        /// </remarks>
        public ReleaseAllocationValidationResult? AllocatedItems { get; set; }

        public int XlDocumentId { get; set; }
    }

    /// <summary>
    /// Represents a single item in Agilero Issue Document
    /// Contains complete information about issued product including series, expiration and warehouse
    /// </summary>
    public class AgileroIssueDocumentItem
    {
        /// <summary>
        /// Quantity realized/issued
        /// </summary>
        public decimal QuantityRealized { get; set; }

        /// <summary>
        /// Source article ID in Agilero system
        /// </summary>
        public int SourceArticle_Id { get; set; }

        /// <summary>
        /// Expiration date of the product
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Series/batch number of the product
        /// </summary>
        public string SeriesNumber { get; set; } = string.Empty;

        /// <summary>
        /// Warehouse ID where the item is stored
        /// </summary>
        public int Warehouse { get; set; }

        /// <summary>
        /// Article master system ID (product ID in main ERP)
        /// </summary>
        public int ArticleMasterSystemId { get; set; }

        /// <summary>
        /// Article code (SKU) of the specific variant
        /// </summary>
        public string ArticleCode { get; set; } = string.Empty;

        /// <summary>
        /// Parent article code (main product SKU)
        /// </summary>
        public string ParentArticleCode { get; set; } = string.Empty;
    }
}
