using Clt.Api.Api.Errors;
using Clt.Api.Api.Middleware;
using FluentValidation;
using Microsoft.OpenApi;
using Clt.Api.Infrastructure.Persistence;
using Clt.Api.Api.Endpoints;
using Clt.Api.Application.Users.Commands;
using Clt.Api.Application.Users.Queries;
using Clt.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Clt.Api.Application.Addresses.Commands;
using Clt.Api.Application.Addresses.Queries;
using Clt.Api.Application.Currencies.Commands;
using Clt.Api.Application.Currencies.Queries;
using Clt.Api.Application.CurrencyConversion;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPersistence(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<BulkCreateUsersHandler>();
builder.Services.AddScoped<UpdateUserHandler>();
builder.Services.AddScoped<DeleteUserHandler>();
builder.Services.AddScoped<GetUserHandler>();
builder.Services.AddScoped<ListUsersHandler>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<CreateAddressHandler>();
builder.Services.AddScoped<UpdateAddressHandler>();
builder.Services.AddScoped<DeleteAddressHandler>();
builder.Services.AddScoped<ListAddressesHandler>();
builder.Services.AddScoped<CreateCurrencyHandler>();
builder.Services.AddScoped<ListCurrenciesHandler>();
builder.Services.AddScoped<ConvertCurrencyHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CLT Technical Test API", Version = "v1" });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Description = "Development key: clt-local-test-key"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("ApiKey", document)] = []
    });
});

var app = builder.Build();
await app.Services.InitializeDatabaseAsync();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
    await Results.Problem(statusCode: context.HttpContext.Response.StatusCode)
        .ExecuteAsync(context.HttpContext));
// Documentation is public so the evaluator can open it and use Authorize.
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapUserEndpoints();
app.MapAddressEndpoints();
app.MapCurrencyEndpoints();
app.Run();

public partial class Program;
