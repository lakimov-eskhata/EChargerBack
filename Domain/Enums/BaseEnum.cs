using System.Reflection;

namespace Domain.Enums;

public class BaseEnum<TEnum, TValue> where TEnum : BaseEnum<TEnum, TValue>
{
    private static readonly List<TEnum> _list = new List<TEnum>();
    public string Name { get; }
    public TValue Value { get; }

    protected BaseEnum(TValue val, string name)
    {
        Value = val;
        Name = name;

        var item = this as TEnum;
        List.Add(item!);
    }

    public static TEnum? FromName(string name)
    {
        return List.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static TEnum? FromValue(TValue value)
    {
        // Can't use == to compare generics unless we constrain TValue to "class", which we don't want because then we couldn't use int.
        return List.FirstOrDefault(item => EqualityComparer<TValue>.Default.Equals(item.Value, value));
    }

    public override string ToString() => $"{Name} ({Value})";

    // Despite analysis tool warnings, we want this static bool to be on this generic type (so that each TEnum has its own bool).
    private static bool _invoked;

    public static List<TEnum> List
    {
        get
        {
            if (!_invoked)
            {
                _invoked = true;
                // Force invocaiton/initialization by calling one of the derived members.
                typeof(TEnum).GetProperties(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(p => p.PropertyType == typeof(TEnum))?.GetValue(null, null);
            }

            return _list;
        }
    }

    public static IDictionary<string, TEnum> Dictionary
    {
        get
        {
            var propertyInfos = typeof(TEnum).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(TEnum))
                .ToList();

            return propertyInfos.
                ToDictionary(propertyInfo => propertyInfo.Name, propertyInfo => propertyInfo.GetValue(null, null) as TEnum)!;
        }
    }
}
