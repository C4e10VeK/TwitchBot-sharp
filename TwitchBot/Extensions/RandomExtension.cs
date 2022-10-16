namespace TwitchBot.Extensions;

public static class RandomExtension
{
    public static float NextFloat(this Random random, float max, float min)
    {
        return random.NextSingle() * (max - min) + min;
    }
}