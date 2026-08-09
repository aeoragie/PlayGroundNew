using PlayGround.Shared.Primitives;

namespace PlayGround.Shared.Result;

public abstract class DetailCode : IEquatable<DetailCode>
{
    public ResultCodes Category { get; }
    public int Value { get; }
    public string Name { get; }
    public string DefaultMessage { get; }

    protected DetailCode(ResultCodes category, int value, string name, string message)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Panic.Fail("DetailCode name cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Panic.Fail($"DetailCode '{name}' message cannot be null or empty.");
        }

        Category = category;
        Value = value;
        Name = name;
        DefaultMessage = message;
    }

    public override string ToString() => $"{Category}:{Value}:{Name}";

    #region Equality Operations

    public static bool operator ==(DetailCode? left, DetailCode? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Category == right.Category && left.Value == right.Value;
    }

    public static bool operator !=(DetailCode? left, DetailCode? right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || GetType() != obj.GetType())
        {
            return false;
        }

        return this == (DetailCode)obj;
    }

    public bool Equals(DetailCode? other)
    {
        return this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Category, Value);
    }

    #endregion

    #region Validation Methods

    public virtual bool IsSuccess => Category == ResultCodes.Success;
    public virtual bool IsError => Category == ResultCodes.Error;
    public virtual bool IsWarning => Category == ResultCodes.Warning;
    public virtual bool IsInformation => Category == ResultCodes.Information;

    #endregion
}
