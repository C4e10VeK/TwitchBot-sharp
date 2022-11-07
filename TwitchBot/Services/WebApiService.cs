using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchBot.Models;

namespace TwitchBot.Services;

public class WebApiService : IWebApiService
{
    private readonly HttpClient _client = new();
    private readonly WebApiConfig _config;
    private string? _token;

    public bool IsAuthorized { get; private set; } = false;

    public WebApiService(IOptions<WebApiConfig> options)
    {
        var config = options.Value;

        _config = config;
        Authorize().Wait();
    }

    public async Task<TReturn?> CallApi<TReturn, TParameter>(string api, HttpMethod method, TParameter parameter)
    {
        var request = new HttpRequestMessage(method, $"{_config.Url}/api{api}")
        {
            Headers =
            {
                {"Authorization", "Basic " + _token}
            }
        };

        request.Content = new StringContent(JsonConvert.SerializeObject(parameter), Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonConvert.DeserializeObject<TReturn>(content);
        return result;
    }
    
    public async Task<TReturn?> CallApi<TReturn>(string api, HttpMethod method)
    {
        var request = new HttpRequestMessage(method, $"{_config.Url}/api{api}")
        {
            Headers =
            {
                {"Authorization", "Basic " + _token}
            }
        };
        
        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonConvert.DeserializeObject<TReturn>(content);
        return result;
    }

    private async Task Authorize()
    {
        var response = await _client.PostAsync($"{_config.Url}/api/Login",
            new StringContent(JsonConvert.SerializeObject(new {login = _config.Login, password = _config.Password}),
                Encoding.UTF8, "application/json"));
        
        if (response.StatusCode != HttpStatusCode.OK)
            return;

        var responseString = await response.Content.ReadAsStringAsync();
        var token = (JObject.Parse(responseString)["token"] ?? throw new InvalidOperationException()).Value<string>();
        
        if (token is null || string.IsNullOrWhiteSpace(token)) return;
        _token = token;
        IsAuthorized = true;
    }
}