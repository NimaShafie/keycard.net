using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyCard.Core.Wrappers
{
    using System.ComponentModel;

    /// <summary>
    /// Response message enum.
    /// </summary>
    public enum ResponseMessageEnum
    {
        /// <summary>
        /// Successful response.
        /// </summary>
        [Description("The Request is successful")]
        Success,

        /// <summary>
        /// Response with exception
        /// </summary>
        [Description("The Request responded with exceptions. Please check and try again.")]
        Exception,

        /// <summary>
        /// Unauthorized access.
        /// </summary>
        [Description("The Request is denied.")]
        UnAuthorized,

        /// <summary>
        /// Response with validations errors.
        /// </summary>
        [Description("The Request responded with validation error(s). Please check and try again")]
        ValidationError,

        /// <summary>
        /// Failed response.
        /// </summary>
        [Description("Unable to process the request.")]
        Failure,
    }
}
