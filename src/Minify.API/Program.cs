using Minify.API.Endpoints;
using Minify.Application;
using Minify.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapUrlEndpoints();
app.MapAnalyticsEndpoints();

app.Run();

public partial class Program { }
