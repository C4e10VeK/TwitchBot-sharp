using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using TwitchBot.Models;

namespace TwitchBot.Services;

public class FeedDbService
{
    private readonly MongoClient _client;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<FeedSmile> _feedEmojis;
    private readonly IMongoCollection<AvailableSmile> _availableSmiles;

    public FeedDbService(IOptions<FeedDBConfig> options)
    {
        var config = options.Value;
        _client = new MongoClient(config.ConnectionString);
        var db = _client.GetDatabase(config.Database);

        _users = db.GetCollection<User>("Users");
        _feedEmojis = db.GetCollection<FeedSmile>(config.Table);
        _availableSmiles = db.GetCollection<AvailableSmile>("AvailableSmiles");
    }

    public async Task<List<User>> GetUsersAsync() => await _users.Find(_ => true).ToListAsync();

    public async Task<List<FeedSmile>> GetSmiles() =>
        await _feedEmojis.Find(_ => true).ToListAsync();
    
    public async Task<List<FeedSmile>> GetSmiles(User user) =>
        await _feedEmojis.Find(e => e.User == user.Name).ToListAsync();
    
    public async Task<List<FeedSmile>> GetSmiles(string? userName) =>
        await _feedEmojis.Find(e => e.User == userName).ToListAsync();

    public async Task<List<string?>> GetAvailableSmiles()
    {
        var list = await _availableSmiles.Find(_ => true).ToListAsync();
        return list.Select(a => a.Name).ToList();
    }

    public async Task<User?> GetUserAsync(ObjectId id)
    {
        if (await _users.CountDocumentsAsync(u => u.Id == id) == 0)
            return null;
        return await _users.Find(u => u.Id == id).SingleAsync();
    }

    public async Task<User?> GetUserAsync(string name)
    {
        var usersCursor = await _users.FindAsync(u => u.Name == name);
        return await _users.CountDocumentsAsync(u => u.Name == name) is 0 ? null : await usersCursor.SingleAsync();
    }

    public async Task<FeedSmile?> GetSmileAsync(User user, string? name)
    {
        var emojiCursor = await _feedEmojis.FindAsync(e => e.User == user.Name && e.Name == name);
        return await _feedEmojis.CountDocumentsAsync(e => e.User == user.Name && e.Name == name) is 0
            ? null
            : await emojiCursor.SingleAsync();
    }

    public async Task<FeedSmile?> GetSmileAsync(ObjectId id)
    {
        var emojiCursor = await _feedEmojis.FindAsync(e => e.Id == id);
        return await _feedEmojis.CountDocumentsAsync(e => e.Id == id) is 0
            ? null
            : await emojiCursor.SingleAsync();
    }

    public async Task UpdateUserAsync(ObjectId id, User? user)
    {
        if (user is null) return;
        await _users.ReplaceOneAsync(u => u.Id == id, user);
    }
    
    public async Task UpdateSmileAsync(ObjectId id, FeedSmile? emoji)
    {
        if (emoji is null) return;
        await _feedEmojis.ReplaceOneAsync(u => u.Id == id, emoji);
    }

    public async Task AddUserAsync(User? user)
    {
        if (user is null) return;
        if ((await GetUsersAsync()).Any(u => u.Name == user.Name)) return;
        await _users.InsertOneAsync(user);
    }

    public async Task AddSmileAsync(FeedSmile? emoji)
    {
        if (emoji is null) return;
        if ((await GetSmiles(emoji.User)).Any(e => e.Name == emoji.Name)) return;
        await _feedEmojis.InsertOneAsync(emoji);
    }

    public async Task AddAvailableSmile(string? name)
    {
        var availableSmile = new AvailableSmile { Name = name };
        if ((await GetAvailableSmiles()).Contains(name)) return;
        await _availableSmiles.InsertOneAsync(availableSmile);

        foreach (var smile in await GetAvailableSmiles())
        {
            foreach (var user in (await GetUsersAsync()).Where(user => !user.FeedSmiles.ContainsKey(smile)))
            {
                await AddSmileAsync(user, smile);
            }
        }
    }
    
    public async Task<FeedSmile?> AddSmileAsync(User user, string? emojiName)
    {
        var emoji = new FeedSmile
        {
            Name = emojiName,
            FeedCount = 0,
            Size = 0,
            Timer = DateTime.UtcNow,
            User = user.Name
        };

        await AddSmileAsync(emoji);
        emoji = await GetSmileAsync(user, emoji.Name);
        if (emoji is null) return emoji;

        user.FeedSmiles.Add(emoji.Name, emoji.Id);
        await UpdateUserAsync(user.Id, user);
        return emoji;
    }
    
    public async Task<User?> AddUser(string name)
    {
        var availableEmojis = await GetAvailableSmiles();
        var user = new User
        {
            Name = name,
            Permission = UserPermission.User
        };
        
        var emojis = await GetSmiles(user);
        if (emojis.Count != availableEmojis.Count)
        {
            foreach (var availableEmoji in availableEmojis.Where(availableEmoji => emojis.All(e => e.Name != availableEmoji)))
            {
                var emoji = await AddSmileAsync(user, availableEmoji) ?? throw new InvalidOperationException();
                emojis.Add(emoji);
            }
        }
        
        await AddUserAsync(user);
        return await GetUserAsync(name);
    }
}