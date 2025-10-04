using FluentValidation;

using MediatR;

namespace KeyCard.Core.Middlewares
{
    public class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
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
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var failures = (await Task.WhenAll(
                        _validators.Select(v => v.ValidateAsync(context, cancellationToken))
                    ))
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (failures.Count != 0)
                {
                    // throw a custom exception (not raw ValidationException)
                    throw new ValidationSummaryException(failures);
                }
            }

            return await next();
        }
    }

    public class ValidationSummaryException : Exception
    {
        public List<string> Errors { get; }

        public ValidationSummaryException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
            : base("Validation failed")
        {
            Errors = failures.Select(f => f.ErrorMessage).ToList();
        }

        // Default constructor
        public ValidationSummaryException()
        {
            Errors = new List<string>();
        }

        // Message-only constructor
        public ValidationSummaryException(string message)
            : base(message)
        {
            Errors = new List<string>();
        }

        // Message + inner exception constructor
        public ValidationSummaryException(string message, Exception inner)
            : base(message, inner)
        {
            Errors = new List<string>();
        }
    }
}
