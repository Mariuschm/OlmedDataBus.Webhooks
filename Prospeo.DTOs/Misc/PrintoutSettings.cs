
namespace Prospeo.DTOs.Misc
{
    public class PrintoutSettings
    {
        /// <summary>
        /// Gets or sets the filter string used to select documents for printing.
        /// </summary>
        public string Filter { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the document type (GIDTYP).
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Gets or sets the printout source ID.
        /// </summary>
        public int SourceId { get; set; }

        /// <summary>
        /// Gets or sets the printout ID.
        /// </summary>
        public int PrintoutId { get; set; }

        /// <summary>
        /// Gets or sets the format ID for the printout.
        /// </summary>
        public int FormatId { get; set; }
        /// <summary>
        /// Gets or sets the filename for the printout.
        /// </summary>
        public string Filename { get; set; } = string.Empty;
    }
}
