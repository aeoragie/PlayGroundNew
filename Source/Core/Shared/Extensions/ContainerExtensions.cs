namespace PlayGround.Shared.Extensions;

public static class ContainerExtensions
{
    public static TValue? GetRandomValue<TKey, TValue>(this Dictionary<TKey, TValue> dict) where TKey : notnull
    {
        if (dict.Count == 0)
        {
            return default;
        }

        return dict.Values.ElementAt(Random.Shared.Next(dict.Count));
    }

    public static TKey? GetRandomKey<TKey, TValue>(this Dictionary<TKey, TValue> dict) where TKey : notnull
    {
        if (dict.Count == 0)
        {
            return default;
        }

        return dict.Keys.ElementAt(Random.Shared.Next(dict.Count));
    }

    public static KeyValuePair<TKey, TValue>? GetRandom<TKey, TValue>(this Dictionary<TKey, TValue> dict) where TKey : notnull
    {
        if (dict.Count == 0)
        {
            return default;
        }

        return dict.ElementAt(Random.Shared.Next(dict.Count));
    }
}
