using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TwitchBot.Models;

public class AvailableSmile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    
    public string Name { get; set; }
}