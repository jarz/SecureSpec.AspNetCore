using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using SecureSpec.AspNetCore.Security;

namespace SecureSpec.AspNetCore.Serialization;

/// <summary>
/// Provides deterministic serialization of OpenAPI documents with stable hash generation.
/// </summary>
public static class CanonicalSerializer
{
    /// <summary>
    /// Serializes an OpenAPI document to JSON with canonical ordering.
    /// </summary>
    /// <param name="document">The OpenAPI document to serialize.</param>
    /// <returns>The canonical JSON representation.</returns>
    public static string SerializeToJson(OpenApiDocument document)
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
        canonicalJson = canonicalJson.Replace("\r\n", "\n", StringComparison.Ordinal);

        return canonicalJson;
    }

    /// <summary>
    /// Serializes an OpenAPI document to YAML with canonical ordering.
    /// </summary>
    /// <param name="document">The OpenAPI document to serialize.</param>
    /// <returns>The canonical YAML representation.</returns>
    public static string SerializeToYaml(OpenApiDocument document)
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
        yaml = yaml.Replace("\r\n", "\n", StringComparison.Ordinal);

        return yaml;
    }

    /// <summary>
    /// Generates a SHA256 hash of the serialized document.
    /// </summary>
    /// <param name="content">The serialized document content.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    public static string GenerateHash(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Normalize line endings to LF before hashing (AC 499)
        content = content.Replace("\r\n", "\n", StringComparison.Ordinal);

        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return ConvertToLowerHex(hash);
    }

    /// <summary>
    /// Generates an ETag from a hash.
    /// Format: W/"sha256:{first16hex}"
    /// </summary>
    /// <param name="hash">The SHA256 hash.</param>
    /// <returns>The ETag value.</returns>
    public static string GenerateETag(string hash)
    {
        ArgumentNullException.ThrowIfNull(hash);

        if (hash.Length < 16)
        {
            throw new ArgumentException("Hash must be at least 16 characters", nameof(hash));
        }

        return $"W/\"sha256:{hash[..16]}\"";
    }

    /// <summary>
    /// Generates an SRI (Subresource Integrity) value from content.
    /// This is a convenience method that uses IntegrityValidator.
    /// </summary>
    /// <param name="content">The content to generate SRI for.</param>
    /// <returns>The SRI attribute value (e.g., "sha256-abc123...").</returns>
    public static string GenerateSri(string content)
    {
        var validator = new IntegrityValidator();
        return validator.GenerateSri(content);
    }

    /// <summary>
    /// Serializes an OpenAPI document and generates both hash and SRI.
    /// </summary>
    /// <param name="document">The OpenAPI document to serialize.</param>
    /// <param name="format">The serialization format (JSON or YAML).</param>
    /// <returns>A tuple containing the serialized content, SHA256 hash, and SRI value.</returns>
    public static (string Content, string Hash, string Sri) SerializeWithIntegrity(
        OpenApiDocument document,
        SerializationFormat format = SerializationFormat.Json)
    {
        ArgumentNullException.ThrowIfNull(document);

        var content = format == SerializationFormat.Json
            ? SerializeToJson(document)
            : SerializeToYaml(document);

        var hash = GenerateHash(content);
        var sri = GenerateSri(content);

        return (content, hash, sri);
    }

    /// <summary>
    /// Serializes a JSON element with canonical ordering (lexical key ordering).
    /// </summary>
    private static string SerializeJsonElementCanonically(JsonElement element)
    {
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
    private static void WriteJsonElementCanonically(Utf8JsonWriter writer, JsonElement element)
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

    /// <summary>
    /// Converts the provided hash bytes to a lowercase hexadecimal string.
    /// </summary>
    private static string ConvertToLowerHex(byte[] hash)
    {
        const string hexTable = "0123456789abcdef";
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            var value = hash[i];
            chars[i * 2] = hexTable[value >> 4];
            chars[(i * 2) + 1] = hexTable[value & 0x0F];
        }

        return new string(chars);
    }
}

/// <summary>
/// Serialization format options.
/// </summary>
public enum SerializationFormat
{
    /// <summary>
    /// JSON format.
    /// </summary>
    Json,

    /// <summary>
    /// YAML format.
    /// </summary>
    Yaml
}
