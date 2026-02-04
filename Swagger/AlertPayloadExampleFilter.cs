using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OpenshiftWebHook.Swagger;

/// <summary>
/// Swagger'da POST /alert/alert için örnek request body gösterir.
/// </summary>
public class AlertPayloadExampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.Name != "ReceiveAlert" || operation.RequestBody?.Content == null)
            return;

        var example = new OpenApiObject
        {
            ["version"] = new OpenApiString("4"),
            ["groupKey"] = new OpenApiString("{}:{alertname=\"HighCPU\"}"),
            ["status"] = new OpenApiString("firing"),
            ["receiver"] = new OpenApiString("webhook"),
            ["externalURL"] = new OpenApiString("http://alertmanager:9093"),
            ["alerts"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["status"] = new OpenApiString("firing"),
                    ["labels"] = new OpenApiObject
                    {
                        ["alertname"] = new OpenApiString("HighCPU"),
                        ["namespace"] = new OpenApiString("production"),
                        ["service"] = new OpenApiString("api"),
                        ["severity"] = new OpenApiString("critical")
                    },
                    ["annotations"] = new OpenApiObject
                    {
                        ["summary"] = new OpenApiString("CPU usage is above 80%")
                    },
                    ["startsAt"] = new OpenApiString("2026-02-04T10:00:00Z"),
                    ["fingerprint"] = new OpenApiString("abc123")
                }
            }
        };

        if (operation.RequestBody?.Content.TryGetValue("application/json", out var mediaType) == true)
            mediaType.Example = example;
    }
}
