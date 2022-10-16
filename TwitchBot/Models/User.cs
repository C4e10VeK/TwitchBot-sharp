using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TwitchBot.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    public string? Name { get; set; }
    public UserPermission Permission { get; set; }

    public bool IsBanned { get; set; } = false;

    public Dictionary<string?, ObjectId> FeedSmiles { get; set; } = new();
}