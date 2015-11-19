using System;
using System.Collections.Generic;
using System.Text;

namespace QMunicate.Controls
{
    public enum ValidationState
    {
        /// <summary>
        /// This means that the text has not be checked if it is valid.
        /// </summary>
        NotValidated,

        /// <summary>
        /// This means that the RadTextBox is currently checking if the text is valid.
        /// </summary>
        Validating,

        /// <summary>
        /// This means that the text has passed the check and is valid.
        /// </summary>
        Valid,

        /// <summary>
        /// This means that the check has passed and the text was found to be invalid.
        /// </summary>
        Invalid
    }
}
