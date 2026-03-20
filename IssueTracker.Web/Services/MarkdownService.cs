using Markdig;

namespace IssueTracker.Web.Services;

/// <summary>Converts Markdown text to sanitized HTML using the Markdig pipeline.</summary>
public class MarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseAutoLinks()
        .UsePipeTables()
        .UseTaskLists()
        .UseEmphasisExtras()
        .Build();

    /// <summary>Renders a Markdown string as HTML. Raw HTML in the input is escaped.</summary>
    public string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;
        return Markdown.ToHtml(markdown, Pipeline);
    }
}
