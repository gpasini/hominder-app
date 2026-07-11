using System.Text.Json;
using System.Text.Json.Serialization;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Infrastructure.Persistence.Serialization;

public sealed class RecurrencePolicyJsonConverter : JsonConverter<RecurrencePolicy>
{
    public override RecurrencePolicy Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var kind = root.GetProperty("kind").GetString();

        return kind switch
        {
            "interval" => new IntervalPolicy(
                root.GetProperty("amount").GetInt32(),
                Enum.Parse<RecurrenceUnit>(root.GetProperty("unit").GetString()!),
                DateOnly.Parse(root.GetProperty("startReference").GetString()!)),
            "monthWindow" => new MonthWindowPolicy(
                root.GetProperty("startMonth").GetInt32(),
                root.GetProperty("endMonth").GetInt32()),
            "fixedDate" => new FixedDatePolicy(DateOnly.Parse(root.GetProperty("dueDate").GetString()!)),
            "oneOff" => new OneOffPolicy(DateOnly.Parse(root.GetProperty("dueDate").GetString()!)),
            _ => throw new JsonException($"Politique de récurrence inconnue: {kind}"),
        };
    }

    public override void Write(Utf8JsonWriter writer, RecurrencePolicy value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case IntervalPolicy interval:
                writer.WriteString("kind", "interval");
                writer.WriteNumber("amount", interval.Amount);
                writer.WriteString("unit", interval.Unit.ToString());
                writer.WriteString("startReference", interval.StartReference.ToString("O"));
                break;
            case MonthWindowPolicy monthWindow:
                writer.WriteString("kind", "monthWindow");
                writer.WriteNumber("startMonth", monthWindow.StartMonth);
                writer.WriteNumber("endMonth", monthWindow.EndMonth);
                break;
            case FixedDatePolicy fixedDate:
                writer.WriteString("kind", "fixedDate");
                writer.WriteString("dueDate", fixedDate.DueDate.ToString("O"));
                break;
            case OneOffPolicy oneOff:
                writer.WriteString("kind", "oneOff");
                writer.WriteString("dueDate", oneOff.DueDate.ToString("O"));
                break;
            default:
                throw new JsonException($"Politique de récurrence non supportée: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }
}
