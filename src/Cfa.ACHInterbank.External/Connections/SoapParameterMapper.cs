using System.Collections;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Cfa.ACHInterbank.External.Connections;

internal static class SoapParameterMapper
{
    public static string BuildActionBody(
        string action,
        IReadOnlyDictionary<string, object?> parameters,
        string actionNamespace)
    {
        var ns = XNamespace.Get(actionNamespace);
        var root = new XElement(ns + action);

        foreach (var parameter in parameters)
        {
            root.Add(BuildElement(ns, parameter.Key, parameter.Value));
        }

        return root.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildElement(XNamespace ns, string name, object? value)
    {
        if (value is null)
        {
            return new XElement(ns + name);
        }

        if (value is string or Guid or DateTime or DateTimeOffset or TimeSpan
            || value.GetType().IsPrimitive || value is decimal)
        {
            return new XElement(ns + name, ConvertToString(value));
        }

        if (value is IEnumerable && !(value is IDictionary))
        {
            var enumerable = (IEnumerable)value;
            var collectionElement = new XElement(ns + name);
            foreach (var item in enumerable)
            {
                collectionElement.Add(BuildElement(ns, "item", item));
            }

            return collectionElement;
        }

        if (value is IDictionary dictionary)
        {
            var objectElement = new XElement(ns + name);
            foreach (DictionaryEntry entry in dictionary)
            {
                objectElement.Add(BuildElement(ns, Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? "item", entry.Value));
            }

            return objectElement;
        }

        var complexElement = new XElement(ns + name);
        var properties = value.GetType().GetProperties()
            .Where(p => p.CanRead)
            .OrderBy(p => p.MetadataToken);

        foreach (var property in properties)
        {
            complexElement.Add(BuildElement(ns, property.Name, property.GetValue(value)));
        }

        return complexElement;
    }

    private static string ConvertToString(object value) => value switch
    {
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        TimeSpan timeSpan => XmlConvert.ToString(timeSpan),
        bool boolean => XmlConvert.ToString(boolean),
        decimal decimalValue => XmlConvert.ToString(decimalValue),
        double doubleValue => XmlConvert.ToString(doubleValue),
        float floatValue => XmlConvert.ToString(floatValue),
        int intValue => XmlConvert.ToString(intValue),
        long longValue => XmlConvert.ToString(longValue),
        short shortValue => XmlConvert.ToString(shortValue),
        uint uintValue => XmlConvert.ToString(uintValue),
        ulong ulongValue => XmlConvert.ToString(ulongValue),
        ushort ushortValue => XmlConvert.ToString(ushortValue),
        Guid guid => guid.ToString("D", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };
}
