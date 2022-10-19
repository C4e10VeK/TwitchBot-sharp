using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TwitchBot.Models;

public class Smile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    public string? Name { get; set; }
    
    [BsonRepresentation(BsonType.Double, AllowTruncation = true)]
    public double Size { get; set; }
    
    public List<string> Users { get; set; } = new();
}