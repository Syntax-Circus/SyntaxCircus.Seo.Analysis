using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace SyntaxCircus.Seo.Analysis;

internal static class DocumentPreparation
{
    internal const int MaximumHtmlCharacters = 500_000;
    internal const int MaximumNestingDepth = 256;
    private const string Excluded = "script,style,nav,footer,[hidden],[aria-hidden='true'],[data-seo-exclude],[data-unplaced],[data-inactive-theme],[data-seo-scope='chrome']";
    private const string NonProse = "pre,code,[data-generated],[data-seo-prose='false']";
    internal static SeoSnapshot Prepare(SeoDocumentInput input, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.ContentHtml);
        if (input.ContentHtml.Length > MaximumHtmlCharacters) throw new ArgumentException("Content exceeds 500,000 characters.", nameof(input));
        token.ThrowIfCancellationRequested();
        using var document = new HtmlParser().ParseDocument(input.ContentHtml);
        var body = document.Body!;
        ValidateDepth(body, token);
        foreach (var node in body.QuerySelectorAll(Excluded)) { token.ThrowIfCancellationRequested(); node.Remove(); }
        var headings = body.QuerySelectorAll("h1,h2,h3,h4,h5,h6")
            .Select(x => new SeoHeading(x.LocalName[1] - '0', Text(x), Section(x))).ToImmutableArray();
        var images = body.QuerySelectorAll("img").Select(x => new SeoImage(x.GetAttribute("alt"), Section(x))).ToImmutableArray();
        var content = body.InnerHtml;
        foreach (var node in body.QuerySelectorAll(NonProse)) { token.ThrowIfCancellationRequested(); node.Remove(); }
        var paragraphs = body.QuerySelectorAll("p").Select(x => new SeoParagraph(Text(x), Section(x))).ToImmutableArray();
        var snapshot = new SeoSnapshot("", input.Title.Trim(), input.MetaDescription.Trim(), input.CanonicalUrl.Trim(),
            input.Slug.Trim(), string.IsNullOrWhiteSpace(input.FocusPhrase) ? null : input.FocusPhrase.Trim(),
            input.Locale, input.Purpose, content, body.InnerHtml, Text(body), headings, images, paragraphs);
        token.ThrowIfCancellationRequested();
        return snapshot with { Id = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(snapshot))) };
    }
    private static string? Section(IElement element) => element.Closest("[data-section-id]")?.GetAttribute("data-section-id");
    private static void ValidateDepth(INode body, CancellationToken token)
    {
        var pending = new Stack<(INode Node, int Depth)>();
        pending.Push((body, 0));
        while (pending.TryPop(out var next))
        {
            token.ThrowIfCancellationRequested();
            if (next.Depth > MaximumNestingDepth) throw new ArgumentException("Content nesting exceeds 256 levels.");
            foreach (var child in next.Node.ChildNodes) pending.Push((child, next.Depth + 1));
        }
    }
    private static string Text(INode node)
    {
        var builder = new StringBuilder();
        Append(node, builder);
        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }
    private static void Append(INode node, StringBuilder builder)
    {
        if (node is IText text) { builder.Append(text.Data); return; }
        var block = node is IElement e && e.LocalName is "p" or "div" or "section" or "article" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "li" or "br" or "blockquote" or "tr";
        if (block) builder.Append(' ');
        foreach (var child in node.ChildNodes) Append(child, builder);
        if (block) builder.Append(' ');
    }
}
