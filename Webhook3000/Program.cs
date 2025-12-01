using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Force Kestrel to listen on port 3000
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(3000);
});

var app = builder.Build();

const string VERIFY_TOKEN = "iamthebestthirtytwocharactertokenplus";

// GET /webhook verification
app.MapGet("/webhook", (HttpRequest request) =>
{
    var mode = request.Query["hub.mode"];
    var token = request.Query["hub.verify_token"];
    var challenge = request.Query["hub.challenge"];

    if (mode == "subscribe" && token == VERIFY_TOKEN)
    {
        return Results.Ok(challenge.ToString());
    }

    return Results.StatusCode(403);
});

// POST /webhook incoming events
app.MapPost("/webhook", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        Console.WriteLine("INCOMING WEBHOOK: " + body);

        var data = JsonSerializer.Deserialize<JsonElement>(body);

        if (data.TryGetProperty("entry", out var entryArray) && entryArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in entryArray.EnumerateArray())
            {
                if (entry.TryGetProperty("changes", out var changesArray))
                {
                    foreach (var change in changesArray.EnumerateArray())
                    {
                        if (change.TryGetProperty("value", out var value) &&
                            value.TryGetProperty("messages", out var messagesArray))
                        {
                            foreach (var msg in messagesArray.EnumerateArray())
                            {
                                var fromNumber = msg.TryGetProperty("from", out var fromProp) ? fromProp.GetString() : "";
                                var text = msg.TryGetProperty("text", out var textProp)
                                           && textProp.TryGetProperty("body", out var bodyProp)
                                           ? bodyProp.GetString()
                                           : "";

                                Console.WriteLine($"Message from {fromNumber}: {text}");
                            }
                        }
                    }
                }
            }
        }

        return Results.Ok("EVENT_RECEIVED");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error handling webhook: " + ex.Message);
        return Results.Ok("OK");
    }
});

app.Run();
