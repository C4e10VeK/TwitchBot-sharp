using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using WebhookDiscord.Models.Embed;

namespace WebhookDiscord.Converters;

public class EmbedTypeConverter : JsonConverter<EmbedType>
{
    public override void WriteJson(JsonWriter writer, EmbedType value, JsonSerializer serializer)
    {
         writer.WriteValue(value.ToString().ToLower());
    }

    [Obsolete("Obsolete")]
    public override EmbedType ReadJson(JsonReader reader, Type objectType, EmbedType existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.Value is not string val)
            throw new JsonSchemaException(objectType.Name);
        
        if (!Enum.TryParse(val, out existingValue)) 
            throw new ArgumentException();

        return existingValue;
    }
}