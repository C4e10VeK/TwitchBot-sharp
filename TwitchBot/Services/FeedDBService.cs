using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using TwitchBot.Models;

namespace TwitchBot.Services;

public class FeedDbService
{
    private readonly MongoClient _client;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Smile> _smiles;
    private readonly IMongoCollection<Anime> _animes;

    public FeedDbService(IOptions<FeedDBConfig> options)
    {
        var config = options.Value;
        _client = new MongoClient(config.ConnectionString);
        var db = _client.GetDatabase(config.Database);

        _users = db.GetCollection<User>("Users");
        _smiles = db.GetCollection<Smile>(config.Table);
        _animes = db.GetCollection<Anime>("Animes");
    }

    public async Task<List<User>> GetUsersAsync() => await _users.Find(_ => true).ToListAsync();

    public async Task<List<Smile>> GetSmiles() =>
        await _smiles.Find(_ => true).ToListAsync();
    
    public async Task<List<Smile>> GetSmiles(string user)
    {
        return await _smiles.Find(s => s.Users.Contains(user)).ToListAsync();
    }

    public async Task<List<Anime>> GetAnimes() => await _animes.Find(_ => true).ToListAsync();

    public async Task<List<string?>> GetAvailableSmiles()
    {
        var list = await _smiles.Find(_ => true).ToListAsync();
        return list.Select(a => a.Name).ToList();
    }

    public async Task<User?> GetUser(ObjectId id)
    {
        var cursor = await _users.FindAsync(u => u.Id == id);
        return await _users.CountDocumentsAsync(u => u.Id == id) > 0 ? await cursor.SingleAsync() : null;
    }
    
    public async Task<User?> GetUser(string name)
    {
        var cursor = await _users.FindAsync(u => u.Name == name);
        return await _users.CountDocumentsAsync(u => u.Name == name) > 0 ? await cursor.SingleAsync() : null;
    }
    
    public async Task<Smile?> GetSmile(ObjectId id)
    {
        var cursor = await _smiles.FindAsync(s => s.Id == id);
        return await _smiles.CountDocumentsAsync(s => s.Id == id) > 0 ? await cursor.SingleAsync() : null;
    }
    
    public async Task<Smile?> GetSmile(string name)
    {
        var cursor = await _smiles.FindAsync(s => s.Name == name);
        return await _smiles.CountDocumentsAsync(s => s.Name == name) > 0 ? await cursor.SingleAsync() : null;
    }
    
    public async Task<Anime?> GetAnime(string name)
    {
        var cursor = await _animes.FindAsync(s => s.User == name);
        return await _animes.CountDocumentsAsync(s => s.User == name) > 0 ? await cursor.SingleAsync() : null;
    }

    public async Task AddUser(User? user)
    {
        if (user is null) return;
        await _users.InsertOneAsync(user);
    }

    public async Task AddSmile(Smile? smile)
    {
        if (smile is null) return;
        await _smiles.InsertOneAsync(smile);
    }
    
    public async Task AddSmile(Anime? anime)
    {
        if (anime is null) return;
        await _animes.InsertOneAsync(anime);
    }

    public async Task UpdateUser(ObjectId id, User? user)
    {
        if (user is null) return;
        await _users.ReplaceOneAsync(u => u.Id == id, user);
    }

    public async Task<Smile?> UpdateSmile(ObjectId id, Smile? smile)
    {
        if (smile is null) return null;
        await _smiles.ReplaceOneAsync(s => s.Id == id, smile);
        return await GetSmile(id);
    }
    
    public async Task<Anime?> UpdateAnime(ObjectId id, Anime? anime)
    {
        if (anime is null) return null;
        await _animes.ReplaceOneAsync(s => s.Id == id, anime);
        return await GetAnime(anime.User);
    }
    
    public async Task<User?> AddUser(string name)
    {
        var user = new User
        {
            Name = name,
            Permission = UserPermission.User,
            IsBanned = false,
            TimeToFeed = DateTime.UtcNow
        };

        await AddUser(user);
        return await GetUser(name);
    }

    public async Task<Smile?> AddSmile(string name)
    {
        var smile = new Smile
        {
            Name = name,
            Size = 0
        };

        await AddSmile(smile);
        return await GetSmile(name);
    }
    
    public async Task<Anime?> AddAnime(string name)
    {
        var anime = new Anime
        {
            User = name,
            Count = 0
        };

        await _animes.InsertOneAsync(anime);
        return await GetAnime(name);
    }
}