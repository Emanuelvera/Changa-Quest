using ChangaQuest.Presentation.Accounts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddAccountServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapAccountEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program
{
}
