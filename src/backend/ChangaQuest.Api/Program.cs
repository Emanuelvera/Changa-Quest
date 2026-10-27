using ChangaQuest.Presentation.Accounts;
using ChangaQuest.Presentation.Changas;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddAccountServices(builder.Configuration);
builder.Services.AddChangaServices();

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
app.MapChangaEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program
{
}
