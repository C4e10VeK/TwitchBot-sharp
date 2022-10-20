using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TwitchBot.Models;

public class Anime
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    
    public string User { get; set; }
    public int Count { get; set; }
}