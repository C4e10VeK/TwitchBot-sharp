using System.Globalization;
using Newtonsoft.Json;
using WebhookDiscord.Models;

namespace WebhookDiscord.Converters;

public class ColorToHexConverter : JsonConverter<Color>
{
    public override void WriteJson(JsonWriter writer, Color? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;   
        }
        
        var hexStr = $"{value.R:X2}{value.G:X2}{value.B:X2}";
        var hexColor = int.Parse(hexStr, NumberStyles.HexNumber);
        
        writer.WriteValue(hexColor);
    }

    public override Color? ReadJson(JsonReader reader, Type objectType, Color? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.Value is not int hexColor) return null;

        Color color = new Color
        {
            R = (byte) ((byte) ((hexColor >> 16) & 0xFF) / 255.0),
            G = (byte) ((byte) ((hexColor >> 8) & 0xFF) / 255.0),
            B = (byte) ((byte) (hexColor & 0xFF) / 255.0)
        };

        return color;
    }
}