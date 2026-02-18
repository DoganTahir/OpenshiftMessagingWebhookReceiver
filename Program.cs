using OpenshiftWebHook.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "OpenshiftWebHook API", Version = "v1" });
    c.OperationFilter<OpenshiftWebHook.Swagger.AlertPayloadExampleFilter>();
});

// Register services
builder.Services.AddHttpClient<ITokenService, TokenService>();
builder.Services.AddHttpClient<ISmsService, SmsService>();

// Configure JSON options for Alertmanager payload
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline - Swagger her ortamda açık
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenshiftWebHook API v1");
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Log.Information("OpenshiftWebHook service starting...");

app.Run();
