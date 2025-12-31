using Prospeo.DTOs.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Prospeo.DTOs.Misc
{
    /// <summary>
    /// Data Transfer Object for ping/health check operations.
    /// Provides information about the system's license holder, version, and current server time.
    /// </summary>
    /// <remarks>
    /// This DTO is typically used for service health checks and basic system information retrieval.
    /// It returns non-sensitive information that can be used to verify service availability
    /// and version compatibility.
    /// </remarks>
    public class PingDto : DTOModelBase
    {
        /// <summary>
        /// Private backing field for the <see cref="RegisteredFor"/> property.
        /// </summary>
        private string company = string.Empty;

        #region PROPERTIES

        /// <summary>
        /// Gets or sets the name of the license holder.
        /// </summary>
        /// <value>
        /// The company or organization name that holds the license for this system.
        /// Defaults to <see cref="string.Empty"/> if not set.
        /// </value>
        public string RegisteredFor
        {
            get => company;
            set => company = value;
        }

        /// <summary>
        /// Gets the author of the system.
        /// </summary>
        /// <value>
        /// Always returns "Prospeo Sp. z o.o." as the system author.
        /// </value>
        public string Author => "Prospeo Sp. z o.o.";

        /// <summary>
        /// Gets the current server time as a string.
        /// </summary>
        /// <value>
        /// The current date and time from the server in default string format.
        /// This value is calculated dynamically each time the property is accessed.
        /// </value>
        /// <remarks>
        /// This property can be used to verify server availability and check for time synchronization issues.
        /// </remarks>
        public string CurrentTime
        {
            get => DateTime.Now.ToString();
        }

        /// <summary>
        /// Gets the version number of the executing assembly.
        /// </summary>
        /// <value>
        /// The full version string (Major.Minor.Build.Revision) of the assembly containing this class.
        /// </value>
        /// <remarks>
        /// This version information can be used to verify compatibility between client and server components.
        /// </remarks>
        public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        #endregion
    }
}
