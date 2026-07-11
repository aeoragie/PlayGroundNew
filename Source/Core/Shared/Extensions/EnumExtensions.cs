using System.ComponentModel;

namespace PlayGround.Shared.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null)
        {
            return value.ToString();
        }

        var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
        if (attribute == null)
        {
            return value.ToString();
        }
        else
        {
            return attribute.Description;
        }
    }
}
