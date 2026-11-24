using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(ConfigureJson);

WebApplication app = builder.Build();
app.MapControllers();
app.Run("http://localhost:5000");

static void ConfigureJson(JsonOptions options)
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
}