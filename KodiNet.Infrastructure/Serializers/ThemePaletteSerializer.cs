using KodiNet.Application.DTOs;
using KodiNet.Domain.Interfaces.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KodiNet.Infrastructure.Serializers;

public sealed class ThemePaletteSerializer : IThemePaletteSerializer<ThemePaletteDto>
{
    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true, // For a readable JSON
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, //  To follow the camelCase convention
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Ignore null properties
    };

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    public string ToJson(ThemePaletteDto classObject)
        => JsonSerializer.Serialize(classObject, _writeOptions);

    public ThemePaletteDto ToObject(string json)
        => JsonSerializer.Deserialize<ThemePaletteDto>(json, _readOptions)
            ?? throw new NullReferenceException("Failed to deserialize ThemePalette.");
}
