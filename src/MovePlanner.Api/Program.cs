using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using MovePlanner.Api.Endpoints;
using MovePlanner.Application;
using MovePlanner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();
app.MapEndpoints();

await app.RunAsync();

public partial class Program
{
    [ExcludeFromCodeCoverage]
    protected Program()
    {
    }
}

