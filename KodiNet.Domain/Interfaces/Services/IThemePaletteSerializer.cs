namespace KodiNet.Domain.Interfaces.Services;

public interface IThemePaletteSerializer<T>
{
    /// <summary>Serialize the object to a JSON string.</summary>
    /// <param name="classObject">The object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    string ToJson(T classObject);

    /// <summary>Deserialize a JSON string to an object.</summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    T ToObject(string json);
}
