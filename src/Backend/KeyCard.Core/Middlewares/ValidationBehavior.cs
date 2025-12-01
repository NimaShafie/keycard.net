// ============================================================================
// VALIDATION BEHAVIOR - AUTOMATIC INPUT VALIDATION
// this is MediatR pipeline magic! runs before every command handler
// checks if the request data is valid before we do any business logic
// no more "if (email is null) throw" in every handler!
// ============================================================================

using FluentValidation;

using KeyCard.Core.Wrappers;

using MediatR;

namespace KeyCard.Core.Middlewares
{
    /// <summary>
    /// MediatR pipeline behavior that validates all requests automatically
    /// Works with FluentValidation - define your rules once, they run everywhere
    /// 
    /// How it works:
    /// 1. Request comes in (e.g., CreateBookingCommand)
    /// 2. This behavior runs BEFORE the handler
    /// 3. Finds all validators for this request type
    /// 4. Runs all validations
    /// 5. If any fail → throws exception, handler never runs
    /// 6. If all pass → continues to handler
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        // all validators registered for this request type
        // injected by DI - we registered them in Program.cs
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // if there are validators for this request type...
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                
                // run ALL validators in parallel for speed
                var failures = (await Task.WhenAll(
                        _validators.Select(v => v.ValidateAsync(context, cancellationToken))
                    ))
                    .SelectMany(r => r.Errors)  // collect all errors
                    .Where(f => f != null)
                    .Select(x => new ValidationError(x.PropertyName, x.ErrorMessage))
                    .ToList();

                // if any validation failed, stop here!
                // dont let bad data reach the business logic
                if (failures.Count != 0)
                {
                    // custom exception gets caught by response middleware
                    // returns nice error response to client
                    throw new CustomValidationException(failures);
                }
            }

            // all validations passed! continue to the actual handler
            return await next();
        }
    }
}
