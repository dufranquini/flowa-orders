using OrderGenerator.Api.Fix;
using OrderGenerator.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IQuickFixSession, QuickFixSession>();
builder.Services.AddSingleton<FixOrderGateway>();
builder.Services.AddSingleton<IOrderGateway>(sp => sp.GetRequiredService<FixOrderGateway>());
builder.Services.AddSingleton<IOrderHistoryRepository, InMemoryOrderHistoryRepository>();
builder.Services.AddHostedService<FixInitiatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
