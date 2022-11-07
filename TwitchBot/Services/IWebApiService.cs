namespace TwitchBot.Services;

public interface IWebApiService
{
    Task<TReturn?> CallApi<TReturn, TParameter>(string api, HttpMethod method, TParameter parameter);
    Task<TReturn?> CallApi<TReturn>(string api, HttpMethod method);
}