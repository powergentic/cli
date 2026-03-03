using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Fetches content from web URLs.
/// </summary>
public sealed partial class WebFetchTool : IAgentTool
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            { "User-Agent", "PGA/1.0 (Powergentic CLI)" }
        }
    };

    public string Name => "web_fetch";
    public string Description => "Fetch the text content of a web page from a URL. Returns the main text content, stripping HTML tags.";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.ReadOnly;

    public AIFunction ToAIFunction() => AIFunctionFactory.Create(FetchAsync, new AIFunctionFactoryOptions
    {
        Name = Name,
        Description = Description
    });

    [Description("Fetch content from a URL. Returns the text content of the page.")]
    private async Task<string> FetchAsync(
        [Description("The URL to fetch content from.")] string url,
        [Description("Optional query to help focus on relevant content from the page.")] string? query = null)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                return "Error: Invalid URL. Must be an http or https URL.";
            }

            var response = await HttpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var content = await response.Content.ReadAsStringAsync();

            if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
                content = StripHtml(content);

            if (content.Length > 50000)
                content = content[..50000] + "\n... (content truncated)";

            return content;
        }
        catch (HttpRequestException ex)
        {
            return $"Error fetching URL: {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "Error: Request timed out.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private static string StripHtml(string html)
    {
        // Remove script and style blocks
        html = ScriptStyleRegex().Replace(html, " ");
        // Remove HTML tags
        html = TagRegex().Replace(html, " ");
        // Decode HTML entities
        html = System.Net.WebUtility.HtmlDecode(html);
        // Normalize whitespace
        html = WhitespaceRegex().Replace(html, " ").Trim();
        // Collapse multiple newlines
        html = MultiNewlineRegex().Replace(html, "\n\n");

        return html;
    }

    [GeneratedRegex(@"<(script|style)[^>]*>[\s\S]*?</\1>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptStyleRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultiNewlineRegex();
}
