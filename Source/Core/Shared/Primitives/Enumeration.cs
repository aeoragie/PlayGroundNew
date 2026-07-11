using System.Reflection;

namespace PlayGround.Shared.Primitives;

/// <summary>
/// 스마트 Enum 기본 클래스
/// </summary>
public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Lazy<Dictionary<int, TEnum>> EnumerationsDictionary =
        new(() => GetAllEnumerations().ToDictionary(e => e.Value));

    public int Value { get; }
    public string Name { get; }

    protected Enumeration(int value, string name)
    {
        Value = value;
        Name = name;
    }

    public static TEnum? FromValue(int value)
    {
        return EnumerationsDictionary.Value.TryGetValue(value, out var enumeration)
            ? enumeration
            : null;
    }

    public static TEnum? FromName(string name)
    {
        return EnumerationsDictionary.Value.Values
            .FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyCollection<TEnum> GetAll()
    {
        return EnumerationsDictionary.Value.Values.ToList().AsReadOnly();
    }

    public bool Equals(Enumeration<TEnum>? other)
    {
        if (other is null)
        {
            return false;
        }

        return GetType() == other.GetType() && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Enumeration<TEnum> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Name;
    }

    private static IEnumerable<TEnum> GetAllEnumerations()
    {
        var enumerationType = typeof(TEnum);
        var fields = enumerationType.GetFields(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields
            .Select(f => f.GetValue(null))
            .OfType<TEnum>();
    }
}
