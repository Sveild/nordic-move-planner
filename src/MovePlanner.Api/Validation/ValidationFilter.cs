using FluentValidation;
using FluentValidation.Results;

namespace MovePlanner.Api.Validation;

/// <summary>
/// Validates the request an endpoint was called with, using the validator registered for
/// <typeparamref name="TRequest"/>. A request type without a validator passes through untouched, so
/// endpoints only pay for the validation they opt in to by having one.
/// </summary>
/// <typeparam name="TRequest">The request type the endpoint binds.</typeparam>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
{
    private readonly IValidator<TRequest>? _validator;

    /// <summary>Resolves the validator of <typeparamref name="TRequest"/>, if one is registered.</summary>
    /// <param name="services">The request services, which hold the validators.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public ValidationFilter(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _validator = services.GetService<IValidator<TRequest>>();
    }

    /// <summary>
    /// Validates the request and invokes the rest of the pipeline when it is valid. An invalid
    /// request short-circuits the endpoint with a 400 problem response that lists the field errors.
    /// </summary>
    /// <param name="context">The context of the endpoint filter invocation.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>The response of the endpoint, or a problem response describing the invalid fields.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.
    /// </exception>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (_validator is null || context.Arguments.OfType<TRequest>().FirstOrDefault() is not { } request)
        {
            return await next(context);
        }

        ValidationResult result = await _validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (result.IsValid)
        {
            return await next(context);
        }

        return Results.ValidationProblem(result.ToDictionary());
    }
}
