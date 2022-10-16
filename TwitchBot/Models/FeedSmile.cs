using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TwitchBot.Models;

public class FeedSmile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public int FeedCount { get; set; }
    public float Size { get; set; }
    public DateTime Timer { get; set; }
    
    public string User { get; set; }
}