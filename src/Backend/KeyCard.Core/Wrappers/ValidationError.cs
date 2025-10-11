using Newtonsoft.Json;


// to suppress possibly null reference warnings.
#pragma warning disable CS8601, CS8618

namespace KeyCard.Core.Wrappers
{
    /// <summary>
    /// Validation error.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class.
        /// </summary>
        /// <param name="field"> Field.</param>
        /// <param name="message"> Message.</param>
        public ValidationError(string field, string message)
        {
            this.Field = field != string.Empty ? field : null;
            this.Message = message;
        }

        /// <summary>
        /// Gets field.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Field { get; }

        /// <summary>
        /// Gets message.
        /// </summary>
        public string Message { get; }
    }
}
