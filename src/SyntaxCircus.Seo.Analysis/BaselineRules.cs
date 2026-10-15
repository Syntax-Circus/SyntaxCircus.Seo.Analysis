using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace SyntaxCircus.Seo.Analysis;

internal static class BaselineRules
{
    internal static readonly HashSet<string> OwnedKeys = ["metadata.title", "metadata.description", "structure.headings", "structure.image-alt",
        "phrase.title", "phrase.description", "phrase.introduction", "phrase.density", "writing.sentence-length", "writing.paragraph-length", "writing.repeated-words"];
    internal static bool English(string locale) => locale.Equals("en", StringComparison.OrdinalIgnoreCase) || locale.StartsWith("en_", StringComparison.OrdinalIgnoreCase) || locale.StartsWith("en-", StringComparison.OrdinalIgnoreCase);
    internal static ImmutableArray<SeoFinding> Analyze(SeoSnapshot s)
    {
        var results = ImmutableArray.CreateBuilder<SeoFinding>();
        void Add(string rule, string key, FindingCategory category, string message, string recommendation, string? section = null, FindingSeverity severity = FindingSeverity.Suggestion)
            => results.Add(new("core." + rule, key, category, severity, message, recommendation, SectionId: section));
        if (s.Title.Length == 0) Add("title", "metadata.title", FindingCategory.Metadata, "Add a search title.", "Describe this page in a short, clear title.", severity: FindingSeverity.Warning);
        else if (s.Title.Length > 65) Add("title-length", "metadata.title", FindingCategory.Metadata, "The search title may be shortened in results.", "Review whether the important words appear first.", severity: FindingSeverity.Information);
        if (s.MetaDescription.Length == 0) Add("description", "metadata.description", FindingCategory.Metadata, "Add a search description.", "Briefly explain what visitors will find.", severity: FindingSeverity.Warning);
        else if (s.MetaDescription.Length > 160) Add("description-length", "metadata.description", FindingCategory.Metadata, "The description may be shortened in results.", "Put the useful summary first.", severity: FindingSeverity.Information);
        if (s.Headings.Count(x => x.Level == 1) != 1) Add("main-heading", "structure.headings", FindingCategory.Structure, "Use one main heading.", "Give this page a single descriptive main heading.");
        var previous = 0;
        foreach (var heading in s.Headings)
        {
            if (heading.Level > previous + 1) Add("heading-order", "structure.headings", FindingCategory.Structure, "A heading skips a level.", "Use heading levels in order.", heading.SectionId);
            previous = heading.Level;
        }
        foreach (var image in s.Images.Where(x => x.Alt is null))
            Add("image-alt", "structure.image-alt", FindingCategory.Structure, "An image has no alternative text.", "Describe meaningful images; use an empty alt attribute for decoration.", image.SectionId);
        if (!English(s.Locale)) return results.ToImmutable();
        if (s.FocusPhrase is { } phrase)
        {
            bool Has(string text) => text.Contains(phrase, StringComparison.OrdinalIgnoreCase);
            if (!Has(s.Title)) Add("phrase-title", "phrase.title", FindingCategory.FocusPhrase, "The chosen phrase is absent from the title.", "Consider using it if it describes the page naturally.");
            if (!Has(s.MetaDescription)) Add("phrase-description", "phrase.description", FindingCategory.FocusPhrase, "The chosen phrase is absent from the description.", "Consider mentioning the topic naturally.");
            if (!Has(s.Paragraphs.FirstOrDefault()?.Text ?? "")) Add("phrase-introduction", "phrase.introduction", FindingCategory.FocusPhrase, "The opening paragraph does not mention the chosen phrase.", "Make the topic clear near the start.");
            var words = WordCount(s.Prose);
            var occurrences = Regex.Matches(s.Prose, @"(?<!\w)" + Regex.Escape(phrase) + @"(?!\w)", RegexOptions.IgnoreCase).Count;
            if (words >= 30 && (double)occurrences / words > .05)
                Add("phrase-density", "phrase.density", FindingCategory.FocusPhrase, "The chosen phrase appears frequently.", "Remove repetition where it makes the writing awkward.");
        }
        var sentences = Regex.Split(s.Prose, @"[.!?]+\s*").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (sentences.Length > 0 && (double)sentences.Count(x => WordCount(x) > 25) / sentences.Length > .25)
            Add("sentence-length", "writing.sentence-length", FindingCategory.Readability, "Some sentences may be hard to follow.", "Try splitting the longest sentences.");
        foreach (var paragraph in s.Paragraphs.Where(x => WordCount(x.Text) > 150))
            Add("paragraph-length", "writing.paragraph-length", FindingCategory.Readability, "A paragraph is long.", "Split it where the subject changes.", paragraph.SectionId);
        if (Regex.IsMatch(s.Prose, @"\b([\p{L}]+)\s+\1\b", RegexOptions.IgnoreCase))
            Add("repeated-words", "writing.repeated-words", FindingCategory.Readability, "A word is repeated immediately.", "Check whether the repetition is intentional.");
        return results.ToImmutable();
    }
    private static int WordCount(string text) => Regex.Matches(text, @"\b[\p{L}\p{N}]+\b").Count;
}
