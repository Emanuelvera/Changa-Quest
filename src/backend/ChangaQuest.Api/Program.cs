using ChangaQuest.Presentation.Accounts;
using ChangaQuest.Presentation.Changas;
using ChangaQuest.Presentation.Reports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddAccountServices(builder.Configuration);
builder.Services.AddChangaServices();
builder.Services.AddReportServices();

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
app.MapReportEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program
{
}
