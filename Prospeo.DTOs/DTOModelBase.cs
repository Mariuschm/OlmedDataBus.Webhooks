using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Prospeo.DTOs
{
    /// <summary>
    /// Abstract base class for all Data Transfer Objects (DTOs) in the Prospeo system.
    /// Provides automatic initialization of string properties to prevent null reference exceptions.
    /// </summary>
    /// <remarks>
    /// This class uses reflection at construction time to scan all properties and initialize
    /// any writable string properties to <see cref="string.Empty"/>. This ensures that all
    /// string properties have a non-null default value, reducing the need for null checks
    /// throughout the codebase.
    /// 
    /// <para>
    /// All DTOs should inherit from this class to benefit from automatic string initialization
    /// and maintain consistency across the data transfer layer.
    /// </para>
    /// 
    /// <example>
    /// <code>
    /// public class ProductDto : DTOModelBase
    /// {
    ///     public string Name { get; set; }  // Will be initialized to string.Empty
    ///     public int Id { get; set; }       // Not affected by initialization
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public abstract class DTOModelBase
    {
        #region CONSTRUCTORS

        /// <summary>
        /// Initializes a new instance of the <see cref="DTOModelBase"/> class.
        /// </summary>
        /// <remarks>
        /// The constructor automatically calls <see cref="InitNoReferencePropeties"/> to initialize
        /// all string properties to <see cref="string.Empty"/>, preventing null reference exceptions
        /// and ensuring consistent behavior across all derived DTOs.
        /// </remarks>
        public DTOModelBase()
        {
            InitNoReferencePropeties();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Initializes all writable string properties to <see cref="string.Empty"/>.
        /// </summary>
        /// <remarks>
        /// This method uses reflection to enumerate all public properties of the derived class.
        /// For each property that is of type <see cref="string"/> and has a public setter,
        /// the method sets the property value to <see cref="string.Empty"/>.
        /// 
        /// <para>
        /// This approach ensures that:
        /// <list type="bullet">
        /// <item><description>No string property will be null by default</description></item>
        /// <item><description>JSON serialization works consistently without null values</description></item>
        /// <item><description>String concatenation and manipulation is safer without null checks</description></item>
        /// <item><description>API consumers receive empty strings instead of null values</description></item>
        /// </list>
        /// </para>
        /// 
        /// <para>
        /// <strong>Performance Note:</strong> This method is called once per object instantiation
        /// and uses reflection. While reflection has some performance overhead, it only executes
        /// during object construction and provides significant benefits in terms of null safety
        /// and API consistency.
        /// </para>
        /// </remarks>
        private void InitNoReferencePropeties()
        {
            // Get all public instance properties of the current object type
            foreach (PropertyInfo prop in this.GetType().GetProperties())
            {
                // Check if the property is a string type and has a public setter
                if (prop.PropertyType == typeof(string) && prop.CanWrite == true)
                {
                    // Initialize the string property to empty string instead of null
                    prop.SetValue(this, string.Empty);
                }
            }
        }

        #endregion
    }
}
