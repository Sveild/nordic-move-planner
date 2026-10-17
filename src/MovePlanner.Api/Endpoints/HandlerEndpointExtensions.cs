using Microsoft.AspNetCore.Mvc;
using MovePlanner.Api.Validation;
using MovePlanner.Application.Common;

namespace MovePlanner.Api.Endpoints;

/// <summary>
/// Maps endpoints that bind an Api request, map it to an Application command or query, run the
/// matching handler and turn the handler's result into an HTTP response.
/// </summary>
/// <remarks>
/// Every mapped endpoint validates the request with the validator registered for the request type,
/// if there is one, and answers an invalid request with a 400 problem response.
/// </remarks>
public static class HandlerEndpointExtensions
{
    /// <summary>
    /// Maps a GET endpoint that binds <typeparamref name="TRequest"/>, maps it to
    /// <typeparamref name="TQuery"/> and runs the matching query handler.
    /// </summary>
    /// <typeparam name="TRequest">The Api request, whose properties carry the binding attributes.</typeparam>
    /// <typeparam name="TQuery">The query the request maps to.</typeparam>
    /// <typeparam name="TResponse">The type of the value the handler returns on success.</typeparam>
    /// <param name="endpoints">The route builder to map the endpoint onto.</param>
    /// <param name="pattern">The route pattern of the endpoint.</param>
    /// <returns>The builder of the endpoint, so metadata can be attached to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoints"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern"/> is <see langword="null"/>, empty or white space.
    /// </exception>
    public static RouteHandlerBuilder MapGetHandler<TRequest, TQuery, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : IQueryRequest<TQuery>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapGet(pattern, HandleQueryAsync<TRequest, TQuery, TResponse>)
            .AddEndpointFilter<ValidationFilter<TRequest>>();
    }

    /// <summary>
    /// Maps a POST endpoint that binds <typeparamref name="TRequest"/>, maps it to
    /// <typeparamref name="TCommand"/> and runs the matching command handler.
    /// </summary>
    /// <typeparam name="TRequest">The Api request, whose properties carry the binding attributes.</typeparam>
    /// <typeparam name="TCommand">The command the request maps to.</typeparam>
    /// <typeparam name="TResponse">The type of the value the handler returns on success.</typeparam>
    /// <param name="endpoints">The route builder to map the endpoint onto.</param>
    /// <param name="pattern">The route pattern of the endpoint.</param>
    /// <returns>The builder of the endpoint, so metadata can be attached to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoints"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern"/> is <see langword="null"/>, empty or white space.
    /// </exception>
    public static RouteHandlerBuilder MapPostHandler<TRequest, TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : ICommandRequest<TCommand>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapPost(pattern, HandleCommandAsync<TRequest, TCommand, TResponse>)
            .AddEndpointFilter<ValidationFilter<TRequest>>();
    }

    /// <summary>
    /// Maps a PUT endpoint that binds <typeparamref name="TRequest"/>, maps it to
    /// <typeparamref name="TCommand"/> and runs the matching command handler.
    /// </summary>
    /// <typeparam name="TRequest">The Api request, whose properties carry the binding attributes.</typeparam>
    /// <typeparam name="TCommand">The command the request maps to.</typeparam>
    /// <typeparam name="TResponse">The type of the value the handler returns on success.</typeparam>
    /// <param name="endpoints">The route builder to map the endpoint onto.</param>
    /// <param name="pattern">The route pattern of the endpoint.</param>
    /// <returns>The builder of the endpoint, so metadata can be attached to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoints"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern"/> is <see langword="null"/>, empty or white space.
    /// </exception>
    public static RouteHandlerBuilder MapPutHandler<TRequest, TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : ICommandRequest<TCommand>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapPut(pattern, HandleCommandAsync<TRequest, TCommand, TResponse>)
            .AddEndpointFilter<ValidationFilter<TRequest>>();
    }

    /// <summary>
    /// Maps a DELETE endpoint that binds <typeparamref name="TRequest"/>, maps it to
    /// <typeparamref name="TCommand"/> and runs the matching command handler.
    /// </summary>
    /// <typeparam name="TRequest">The Api request, whose properties carry the binding attributes.</typeparam>
    /// <typeparam name="TCommand">The command the request maps to.</typeparam>
    /// <typeparam name="TResponse">The type of the value the handler returns on success.</typeparam>
    /// <param name="endpoints">The route builder to map the endpoint onto.</param>
    /// <param name="pattern">The route pattern of the endpoint.</param>
    /// <returns>The builder of the endpoint, so metadata can be attached to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="endpoints"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern"/> is <see langword="null"/>, empty or white space.
    /// </exception>
    public static RouteHandlerBuilder MapDeleteHandler<TRequest, TCommand, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
        where TRequest : ICommandRequest<TCommand>
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapDelete(pattern, HandleCommandAsync<TRequest, TCommand, TResponse>)
            .AddEndpointFilter<ValidationFilter<TRequest>>();
    }

    private static async Task<IResult> HandleQueryAsync<TRequest, TQuery, TResponse>(
        [AsParameters] TRequest request,
        [FromServices] IQueryHandler<TQuery, TResponse> handler,
        CancellationToken cancellationToken)
        where TRequest : IQueryRequest<TQuery>
    {
        Result<TResponse> result = await handler.HandleAsync(request.ToQuery(), cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> HandleCommandAsync<TRequest, TCommand, TResponse>(
        [AsParameters] TRequest request,
        [FromServices] ICommandHandler<TCommand, TResponse> handler,
        CancellationToken cancellationToken)
        where TRequest : ICommandRequest<TCommand>
    {
        Result<TResponse> result = await handler.HandleAsync(request.ToCommand(), cancellationToken);

        return result.ToHttpResult();
    }
}
