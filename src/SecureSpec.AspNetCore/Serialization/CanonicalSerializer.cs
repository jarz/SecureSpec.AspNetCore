using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SecureSpec.AspNetCore.Serialization;

/// <summary>
/// Provides deterministic serialization of OpenAPI documents with stable hash generation.
/// </summary>
public class CanonicalSerializer
{
    /// <summary>
    /// Serializes an OpenAPI document to JSON with canonical ordering.
    /// </summary>
    /// <param name="document">The OpenAPI document to serialize.</param>
    /// <returns>The canonical JSON representation.</returns>
    public string SerializeToJson(OpenApiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var stringWriter = new StringWriter();
        var openApiWriter = new OpenApiJsonWriter(stringWriter, new OpenApiJsonWriterSettings
        {
            InlineLocalReferences = true,
            InlineExternalReferences = false
        });

        document.SerializeAsV3(openApiWriter);
        var json = stringWriter.ToString();

        // Parse and re-serialize with canonical ordering
        var jsonDoc = JsonDocument.Parse(json);
        var canonicalJson = SerializeJsonElementCanonically(jsonDoc.RootElement);

        // Ensure LF line endings only (no CRLF)
        canonicalJson = canonicalJson.Replace("\r\n", "\n");

        return canonicalJson;
    }

    /// <summary>
    /// Serializes an OpenAPI document to YAML with canonical ordering.
    /// </summary>
    /// <param name="document">The OpenAPI document to serialize.</param>
    /// <returns>The canonical YAML representation.</returns>
    public string SerializeToYaml(OpenApiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var stringWriter = new StringWriter();
        var openApiWriter = new OpenApiYamlWriter(stringWriter, new OpenApiWriterSettings
        {
            InlineLocalReferences = true,
            InlineExternalReferences = false
        });

        document.SerializeAsV3(openApiWriter);
        var yaml = stringWriter.ToString();

        // Ensure LF line endings only (no CRLF)
        yaml = yaml.Replace("\r\n", "\n");

        return yaml;
    }

    /// <summary>
    /// Generates a SHA256 hash of the serialized document.
    /// </summary>
    /// <param name="content">The serialized document content.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    public string GenerateHash(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Normalize line endings to LF before hashing (AC 499)
        content = content.Replace("\r\n", "\n");

        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Generates an ETag from a hash.
    /// Format: W/"sha256:{first16hex}"
    /// </summary>
    /// <param name="hash">The SHA256 hash.</param>
    /// <returns>The ETag value.</returns>
    public string GenerateETag(string hash)
    {
        ArgumentNullException.ThrowIfNull(hash);

        if (hash.Length < 16)
        {
            throw new ArgumentException("Hash must be at least 16 characters", nameof(hash));
        }

        return $"W/\"sha256:{hash[..16]}\"";
    }

    /// <summary>
    /// Serializes a JSON element with canonical ordering (lexical key ordering).
    /// </summary>
    private string SerializeJsonElementCanonically(JsonElement element)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var memoryStream = new MemoryStream();
        using var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions
        {
            Indented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        WriteJsonElementCanonically(writer, element);
        writer.Flush();

        var bytes = memoryStream.ToArray();
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Writes a JSON element with canonical ordering (recursive).
    /// </summary>
    private void WriteJsonElementCanonically(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                // Sort properties lexically for canonical ordering (AC 493)
                var properties = element.EnumerateObject()
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .ToList();
                foreach (var prop in properties)
                {
                    writer.WritePropertyName(prop.Name);
                    WriteJsonElementCanonically(writer, prop.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteJsonElementCanonically(writer, item);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                // Use invariant culture for numeric serialization (AC 45)
                if (element.TryGetInt32(out var intValue))
                {
                    writer.WriteNumberValue(intValue);
                }
                else if (element.TryGetInt64(out var longValue))
                {
                    writer.WriteNumberValue(longValue);
                }
                else if (element.TryGetDouble(out var doubleValue))
                {
                    writer.WriteNumberValue(doubleValue);
                }
                else
                {
                    writer.WriteRawValue(element.GetRawText());
                }

                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }
}
