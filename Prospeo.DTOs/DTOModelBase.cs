using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Prospeo.DTOs
{
    public abstract class DTOModelBase
    {
        #region CONSTRUCTORS

        public DTOModelBase()
        {
            InitNoReferencePropeties();
        }

        #endregion

        #region METHODS

        private void InitNoReferencePropeties()
        {
            foreach (PropertyInfo prop in this.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(string) && prop.CanWrite == true)
                {
                    prop.SetValue(this, string.Empty);
                }
            }
        }

        #endregion
    }
}
