using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Element faktury zakupu
    /// </summary>
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
        /// Kod towaru
        /// </summary>
        [Required]
        public string Kod
        {
            get => code;
            set => code = value;
        }
        /// <summary>
        /// Zakupiona ilość
        /// </summary>
        [Required]
        public decimal Ilosc
        {
            get => quantity;
            set => quantity = value;
        }
        /// <summary>
        /// Cena netto 
        /// </summary>
        [Required]
        public decimal Cena
        {
            get => price;
            set => price = value;
        }
        /// <summary>
        /// Stawka VAT *100
        /// </summary>
        [Required]
        public int StawkaPod
        {
            get => taxRate;
            set => taxRate = value;
        }
        /// <summary>
        /// Grupa podatakowa VAT
        /// </summary>
        [Required]
        public string Vat
        {
            get => taxGroup;
            set => taxGroup = value;
        }
        #endregion
    }
    /// <summary>
    /// Model faktury zakupowej
    /// </summary>
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
        /// Numer źródłowy faktury
        /// </summary>
        [Required]
        public string DokumentObcy
        {
            get => sourceNumber;
            set => sourceNumber = value;
        }
        /// <summary>
        /// Termin płatności w formacie Clarion
        /// </summary>
        [Required]
        public int Termin
        {
            get => paymentTerm;
            set => paymentTerm = value;
        }
        /// <summary>
        /// Typ dokumentu zgodnie z typami XL
        /// </summary>
        [Required]
        public int Typ
        {
            get => documentType;
            set => documentType = value;
        }
        /// <summary>
        /// Akronim dstawcy
        /// </summary>
        [Required]
        public string Akronim
        {
            get => shortCode;
            set => shortCode = value;
        }
        /// <summary>
        /// Opis
        /// </summary>
        public string Opis
        {
            get => description;
            set => description = value;
        }

        /// <summary>
        /// Numer zamówienia powiązanego z fakturą zakupu
        /// </summary>
        public string OrderNumber
        { get; set; } = string.Empty;

        /// <summary>
        /// Elementy dokumentu
        /// </summary>
        [Required]
        public ICollection<InvoiceItem> Items
        {
            get => items;
            set => items = value;
        }

        #endregion
    }
}
