using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MovePlanner.Api.Endpoints;
using MovePlanner.Api.Validation;
using MovePlanner.Application.Common;
using Xunit;

namespace MovePlanner.Api.IntegrationTests;

public sealed class ValidationFilterTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddScoped<IValidator<GreetRequest>, GreetRequestValidator>();
        builder.Services.AddScoped<IQueryHandler<GreetQuery, string>, GreetQueryHandler>();

        _app = builder.Build();

        RouteGroupBuilder dummy = _app.MapGroup("/dummy");
        dummy.MapGetHandler<GreetRequest, GreetQuery, string>("/greet/{name?}");
        dummy.MapGetHandler<PlainRequest, GreetQuery, string>("/plain");

        _app.MapGet("/dummy/elsewhere", () => "elsewhere")
            .AddEndpointFilter<ValidationFilter<GreetRequest>>();

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task An_invalid_request_returns_a_400_with_the_field_errors()
    {
        HttpResponseMessage response =
            await _client.GetAsync(Relative("/dummy/greet"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        FieldErrors? problem =
            await response.Content.ReadFromJsonAsync<FieldErrors>(TestContext.Current.CancellationToken);

        problem.Should().NotBeNull();
        problem.Errors.Should().ContainKey("Name").WhoseValue.Should().NotBeEmpty();
    }

    [Fact]
    public async Task A_valid_request_reaches_the_handler()
    {
        HttpResponseMessage response =
            await _client.GetAsync(Relative("/dummy/greet/Ada"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken))
            .Should().Be("hello:Ada");
    }

    [Fact]
    public async Task A_request_type_without_a_validator_passes_through()
    {
        HttpResponseMessage response =
            await _client.GetAsync(Relative("/dummy/plain?name=Bob"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken))
            .Should().Be("hello:Bob");
    }

    [Fact]
    public async Task An_endpoint_that_does_not_bind_the_request_passes_through()
    {
        HttpResponseMessage response =
            await _client.GetAsync(Relative("/dummy/elsewhere"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Be("elsewhere");
    }

    private static Uri Relative(string path) => new(path, UriKind.Relative);

    private sealed record FieldErrors(Dictionary<string, string[]> Errors);

    private sealed record GreetQuery(string Name);

    private sealed class GreetRequest : IQueryRequest<GreetQuery>
    {
        [FromRoute]
        public string Name { get; set; } = string.Empty;

        public GreetQuery ToQuery() => new(Name);
    }

    private sealed class GreetRequestValidator : AbstractValidator<GreetRequest>
    {
        public GreetRequestValidator()
        {
            RuleFor(request => request.Name).NotEmpty();
        }
    }

    private sealed class PlainRequest : IQueryRequest<GreetQuery>
    {
        [FromQuery]
        public string Name { get; set; } = string.Empty;

        public GreetQuery ToQuery() => new(Name);
    }

    private sealed class GreetQueryHandler : IQueryHandler<GreetQuery, string>
    {
        public Task<Result<string>> HandleAsync(GreetQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(Result.Success($"hello:{query.Name}"));
        }
    }
}
