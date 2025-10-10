using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KeyCard.Core.Wrappers
{
    /// <summary>
    /// API Error response model.
    /// </summary>
    public class ApiError
    {
        public ApiError(string message)
        {
            this.ExceptionMessage = message;
            this.IsError = true;
        }

        public ApiError(ModelStateDictionary modelState)
        {
            this.IsError = true;

            if (modelState != null && modelState.Any(m => m.Value.Errors.Count > 0))
            {
                this.ExceptionMessage = "Please correct the specified validation errors and try again.";

                this.ValidationErrors = modelState.Keys
                    .SelectMany(key => modelState[key].Errors.Select(x => new ValidationError(key, x.ErrorMessage)))
                    .ToList();
            }
        }

        public string Details { get; set; } = default!;
        public string ExceptionMessage { get; set; } = default!;
        public bool IsError { get; set; }
        public IEnumerable<ValidationError> ValidationErrors { get; set; } = new List<ValidationError>();
    }
}
