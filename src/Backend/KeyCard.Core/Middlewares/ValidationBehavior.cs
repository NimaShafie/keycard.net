using FluentValidation;

using KeyCard.Core.Wrappers;

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
                    .Select(x => new ValidationError(x.PropertyName, x.ErrorMessage))
                    .ToList();

                if (failures.Count != 0)
                {
                    // throw a custom exception (not raw ValidationException)
                    throw new CustomValidationException(failures);
                }
            }

            return await next();
        }
    }
}
