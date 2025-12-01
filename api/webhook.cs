using System.Text.Json;
using System.Net;
using System.IO;

public static class WebhookFunction
{
    const string VERIFY_TOKEN = "iamthebestthirtytwocharactertokenplus";

    public static async Task<dynamic> Run(HttpContext context)
    {
        if (context.Request.Method == "GET")
        {
            var mode = context.Request.Query["hub.mode"];
            var token = context.Request.Query["hub.verify_token"];
            var challenge = context.Request.Query["hub.challenge"];

            if (mode == "subscribe" && token == VERIFY_TOKEN)
            {
                return Results.Ok(challenge.ToString());
            }

            return Results.StatusCode(403);
        }

        if (context.Request.Method == "POST")
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            Console.WriteLine("Incoming Webhook: " + body);

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

        return Results.Ok("OK");
    }
}
