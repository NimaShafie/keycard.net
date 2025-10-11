
// to suppress constructor error.
#pragma warning disable CS8618

namespace KeyCard.Core.Wrappers
{
    public class CustomValidationException : Exception
    {
        public CustomValidationException(
           List<ValidationError> errors,
           int statusCode = 500,
           string? message = null)
           : base(message)
        {
            this.StatusCode = statusCode;
            this.Errors = errors;
        }

        public CustomValidationException(Exception ex, int statusCode = 500)
            : base(ex.Message)
        {
            this.StatusCode = statusCode;
        }

        public IEnumerable<ValidationError> Errors { get; set; }
        public int StatusCode { get; set; }
    }
}
