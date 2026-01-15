using Prospeo.DTOs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prospeo.DTOs.Invoice
{
    public class OrderInvoiceDto : DTOModelBase
    {
        public int MasterSystemId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public int Status { get; set; } // e.g., 0 = Pending, 1 = done
        public int Type { get; set; }
        public string Creator { get; set; } = string.Empty;
        public List<OrderInvoiceItemDto> Items { get; set; } = new List<OrderInvoiceItemDto>();
    }
    public class OrderInvoiceItemDto
    {
        public decimal QuantityRealized { get; set; }
        public int SourceArticle_Id { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string SeriesNumber { get; set; } = string.Empty;
        public int Warehouse { get; set; }
        public int ArticleMasterSystemId { get; set; }
        public string ArticleCode { get; set; } = string.Empty;
        public string ParentArticleCode { get; set; } = string.Empty;
    }
}
