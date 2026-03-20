using IssueTracker.Web.Services;

namespace IssueTracker.Tests;

public class MarkdownServiceTests
{
    private readonly MarkdownService _sut = new();

    [Fact]
    public void ToHtml_ConvertsHeadings()
    {
        var result = _sut.ToHtml("# Hello");
        Assert.Contains("<h1>Hello</h1>", result);
    }

    [Fact]
    public void ToHtml_ConvertsBoldAndItalic()
    {
        var result = _sut.ToHtml("**bold** and *italic*");
        Assert.Contains("<strong>bold</strong>", result);
        Assert.Contains("<em>italic</em>", result);
    }

    [Fact]
    public void ToHtml_ConvertsPipeTables()
    {
        var markdown = "| Col1 | Col2 |\n|------|------|\n| A | B |";
        var result = _sut.ToHtml(markdown);
        Assert.Contains("<table>", result);
        Assert.Contains("<td>A</td>", result);
    }

    [Fact]
    public void ToHtml_ConvertsCodeBlocks()
    {
        var markdown = "```\nvar x = 1;\n```";
        var result = _sut.ToHtml(markdown);
        Assert.Contains("<code>", result);
    }

    [Fact]
    public void ToHtml_StripsRawHtml()
    {
        var result = _sut.ToHtml("Hello <script>alert('xss')</script> world");
        Assert.DoesNotContain("<script>", result);
        Assert.Contains("Hello", result);
        Assert.Contains("world", result);
    }

    [Fact]
    public void ToHtml_ConvertsAutoLinks()
    {
        var result = _sut.ToHtml("Visit http://example.com for info");
        Assert.Contains("<a href=\"http://example.com\"", result);
    }

    [Fact]
    public void ToHtml_ConvertsTaskLists()
    {
        var markdown = "- [ ] Unchecked\n- [x] Checked";
        var result = _sut.ToHtml(markdown);
        Assert.Contains("type=\"checkbox\"", result);
    }

    [Fact]
    public void ToHtml_ReturnsEmptyForNullOrWhitespace()
    {
        Assert.Equal(string.Empty, _sut.ToHtml(null!));
        Assert.Equal(string.Empty, _sut.ToHtml(""));
        Assert.Equal(string.Empty, _sut.ToHtml("   "));
    }

    [Fact]
    public void ToHtml_HandlesLargeContent()
    {
        // Simulate BATON-sized content (~3KB)
        var markdown = string.Join("\n\n", Enumerable.Range(1, 50)
            .Select(i => $"## Section {i}\n\nThis is paragraph {i} with **bold** and `code`."));

        var result = _sut.ToHtml(markdown);
        Assert.Contains("<h2>Section 1</h2>", result);
        Assert.Contains("<h2>Section 50</h2>", result);
    }
}
