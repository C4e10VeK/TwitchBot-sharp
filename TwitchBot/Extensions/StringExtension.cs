using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Interfaces;

namespace TwitchBot.Extensions;

public static class StringExtension
{
    public static string GetChannelId(this string name, ITwitchAPI api)
    {

        var res = api.Helix.Users.GetUsersAsync(logins: new List<string> {name}, accessToken: api.Settings.AccessToken)
            .Result;
        return (res?.Users ?? Array.Empty<User>()).Any() ? res?.Users?.First().Id ?? "" : "";
    }
    
    public static string GetChannelName(this string id, ITwitchAPI api)
    {

        var res = api.Helix.Users.GetUsersAsync(ids: new List<string> {id}, accessToken: api.Settings.AccessToken)
            .Result;
        return (res?.Users ?? Array.Empty<User>()).Any() ? res?.Users?.First().Login ?? "" : "";
    }
}