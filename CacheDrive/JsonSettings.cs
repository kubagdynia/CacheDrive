using System.Text.Encodings.Web;
using System.Text.Json;

namespace CacheDrive;

internal static class JsonSettings
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}