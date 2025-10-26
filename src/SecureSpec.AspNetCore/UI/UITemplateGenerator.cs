using SecureSpec.AspNetCore.Configuration;
using System.Globalization;
using System.Text;

namespace SecureSpec.AspNetCore.UI;

/// <summary>
/// Generates HTML templates for the SecureSpec UI.
/// </summary>
public static class UITemplateGenerator
{
    /// <summary>
    /// Generates the main index.html page for the SecureSpec UI.
    /// </summary>
    /// <param name="options">The SecureSpec configuration options.</param>
    /// <returns>The generated HTML content.</returns>
    public static string GenerateIndexHtml(SecureSpecOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var title = options.UI.DocumentTitle ?? "SecureSpec API Documentation";
        var deepLinking = options.UI.DeepLinking ? "true" : "false";
        var displayOperationId = options.UI.DisplayOperationId ? "true" : "false";
        var defaultModelsExpandDepth = options.UI.DefaultModelsExpandDepth;
        var enableFiltering = options.UI.EnableFiltering ? "true" : "false";
        var enableTryItOut = options.UI.EnableTryItOut ? "true" : "false";

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"UTF-8\">");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine(CultureInfo.InvariantCulture, $"  <title>{EscapeHtml(title)}</title>");
        html.AppendLine("  <link rel=\"stylesheet\" href=\"assets/styles.css\">");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <div id=\"securespec-ui\">");
        html.AppendLine("    <header>");
        html.AppendLine(CultureInfo.InvariantCulture, $"      <h1>{EscapeHtml(title)}</h1>");
        html.AppendLine("    </header>");
        html.AppendLine("    <nav id=\"navigation\">");
        html.AppendLine("      <!-- Navigation will be populated by JavaScript -->");
        html.AppendLine("    </nav>");
        html.AppendLine("    <main id=\"content\">");
        html.AppendLine("      <!-- Content will be populated by JavaScript -->");
        html.AppendLine("    </main>");
        html.AppendLine("  </div>");
        html.AppendLine();
        html.AppendLine("  <!-- Configuration -->");
        html.AppendLine("  <script type=\"application/json\" id=\"ui-config\">");
        html.AppendLine("  {");
        html.AppendLine(CultureInfo.InvariantCulture, $"    \"deepLinking\": {deepLinking},");
        html.AppendLine(CultureInfo.InvariantCulture, $"    \"displayOperationId\": {displayOperationId},");
        html.AppendLine(CultureInfo.InvariantCulture, $"    \"defaultModelsExpandDepth\": {defaultModelsExpandDepth},");
        html.AppendLine(CultureInfo.InvariantCulture, $"    \"enableFiltering\": {enableFiltering},");
        html.AppendLine(CultureInfo.InvariantCulture, $"    \"enableTryItOut\": {enableTryItOut}");
        html.AppendLine("  }");
        html.AppendLine("  </script>");
        html.AppendLine();
        html.AppendLine("  <!-- Core application -->");
        html.AppendLine("  <script type=\"module\" src=\"assets/app.js\"></script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    /// <summary>
    /// Escapes HTML special characters.
    /// </summary>
    private static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
    }
}
