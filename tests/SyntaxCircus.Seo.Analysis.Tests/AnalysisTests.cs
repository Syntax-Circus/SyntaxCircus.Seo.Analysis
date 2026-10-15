// Short, in-memory checks; cancellation behavior is exercised with an explicit canceled token below.
#pragma warning disable xUnit1051
using System.Collections.Immutable;
using SyntaxCircus.Seo.Analysis;
using Xunit;

public sealed class AnalysisTests
{
    private readonly ContentSeoAnalyzer analyzer = new();
    private SeoSnapshot Prepare(string html, ContentPurpose purpose = ContentPurpose.General, string? phrase = null, string locale = "en_US")
        => analyzer.Prepare(new(html, "Our dogs", "Meet our dogs and learn about their care.", FocusPhrase: phrase, Purpose: purpose, Locale: locale));

    [Fact] public void Preparation_excludes_chrome_and_separates_generated_content_from_prose()
    {
        var snapshot = Prepare("<nav>Menu</nav><section data-section-id='intro'><h1>Our dogs</h1><p>Hello <b>friends</b>.</p><p>Welcome.</p><div data-generated><h2>Puppies</h2><p>Spot 2026</p></div><pre>code</pre><script>evil()</script><p hidden>Secret</p></section>");
        Assert.Contains("Hello friends.", snapshot.Prose);
        Assert.Contains("Welcome.", snapshot.Prose);
        Assert.DoesNotContain("Spot", snapshot.Prose);
        Assert.DoesNotContain("Secret", snapshot.Prose);
        Assert.DoesNotContain("Menu", snapshot.ContentHtml);
        Assert.DoesNotContain("evil", snapshot.ContentHtml);
        Assert.Equal(2, snapshot.Headings.Length);
        Assert.Equal("intro", snapshot.Headings[0].SectionId);
    }
    [Fact] public void Snapshot_identity_is_deterministic_and_changes_with_metadata_or_purpose()
    {
        var input = new SeoDocumentInput("<p>Hello &amp; welcome", "Title");
        Assert.Equal(analyzer.Prepare(input).Id, analyzer.Prepare(input).Id);
        Assert.NotEqual(analyzer.Prepare(input).Id, analyzer.Prepare(input with { Title = "Other" }).Id);
        Assert.NotEqual(analyzer.Prepare(input).Id, analyzer.Prepare(input with { Purpose = ContentPurpose.Editorial }).Id);
    }
    [Fact] public void Excessive_nesting_is_rejected_without_serializing_the_dom()
        => Assert.Throws<ArgumentException>(() => Prepare(string.Concat(Enumerable.Repeat("<div>", 1000)) + "text" + string.Concat(Enumerable.Repeat("</div>", 1000))));
    [Fact] public async Task Missing_alt_has_location_but_empty_decorative_alt_is_allowed()
    {
        var report = await analyzer.AnalyzeAsync(Prepare("<h1>Our dogs</h1><section data-section-id='photos'><img src='a'><img src='b' alt=''></section>"));
        var finding = Assert.Single(report.Findings.Where(x => x.RuleId == "core.image-alt"));
        Assert.Equal("photos", finding.SectionId);
        Assert.Equal(CoverageState.NotRequested, report.Coverage.Single(x => x.Provider == "companion").State);
    }
    [Fact] public async Task Missing_metadata_and_heading_hierarchy_are_actionable_without_phrase_nags()
    {
        var report = await analyzer.AnalyzeAsync(analyzer.Prepare(new("<h1>A</h1><h3>B</h3>")));
        Assert.Contains(report.Findings, x => x.RuleId == "core.title");
        Assert.Contains(report.Findings, x => x.RuleId == "core.description");
        Assert.Contains(report.Findings, x => x.RuleId == "core.heading-order");
        Assert.DoesNotContain(report.Findings, x => x.Category == FindingCategory.FocusPhrase);
    }
    [Fact] public async Task Explicit_phrase_checks_improve_when_used_naturally()
    {
        var bad = await analyzer.AnalyzeAsync(Prepare("<h1>Dogs</h1><p>Welcome home.</p>", phrase: "puppy care"));
        var good = await analyzer.AnalyzeAsync(analyzer.Prepare(new("<h1>Puppy care</h1><p>Puppy care starts here.</p>", "Puppy care", "Learn about puppy care.", FocusPhrase: "puppy care")));
        Assert.Equal(3, bad.Findings.Count(x => x.Category == FindingCategory.FocusPhrase));
        Assert.DoesNotContain(good.Findings, x => x.Category == FindingCategory.FocusPhrase);
    }
    [Fact] public async Task Unsupported_language_retains_structure_and_reports_writing_gap()
    {
        var report = await analyzer.AnalyzeAsync(Prepare("<h1>A</h1><img>", locale: "fr_FR"));
        Assert.Contains(report.Findings, x => x.RuleId == "core.image-alt");
        Assert.Contains(report.Coverage, x => x.State == CoverageState.UnsupportedLanguage);
    }
    [Fact] public async Task Editorial_purpose_enables_companion_and_preserves_all_core_findings()
    {
        var snapshot = Prepare("<h1>A</h1><img><p>Writing.</p>", ContentPurpose.Editorial);
        var baseline = await analyzer.AnalyzeAsync(snapshot);
        var report = await analyzer.AnalyzeAsync(snapshot, new Fake(snapshot.Id, CoverageState.Complete));
        Assert.All(baseline.Findings, x => Assert.Contains(x, report.Findings));
        Assert.Contains(report.Findings, x => x.Source == "test");
        Assert.Contains(report.CompanionDecisions, x => !x.Included && x.Reason == "Owned by core");
        Assert.Contains(report.CompanionDecisions, x => !x.Included && x.Reason == "Unrecognized assessment");
        Assert.Contains(report.Coverage, x => x.Provider == "test" && x.State == CoverageState.Partial);
    }
    [Theory]
    [InlineData(CoverageState.Failed)] [InlineData(CoverageState.TimedOut)] [InlineData(CoverageState.Unavailable)]
    public async Task Companion_failure_preserves_baseline(CoverageState state)
    {
        var snapshot = Prepare("<h1>A</h1><img>", ContentPurpose.Editorial);
        var report = await analyzer.AnalyzeAsync(snapshot, new Fake(snapshot.Id, state));
        Assert.Contains(report.Findings, x => x.RuleId == "core.image-alt");
        Assert.DoesNotContain(report.Findings, x => x.Source == "test");
        Assert.Contains(report.Coverage, x => x.State == state);
    }
    [Fact] public async Task Stale_companion_is_never_applied()
    {
        var report = await analyzer.AnalyzeAsync(Prepare("<h1>A</h1>", ContentPurpose.Editorial), new Fake("old", CoverageState.Complete));
        Assert.Contains(report.Coverage, x => x.State == CoverageState.Stale);
        Assert.DoesNotContain(report.Findings, x => x.Source == "test");
    }
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task Malformed_companion_preserves_baseline(int kind)
    {
        var report = await analyzer.AnalyzeAsync(Prepare("<h1>A</h1><img>", ContentPurpose.Editorial), new Malformed(kind));
        Assert.Contains(report.Findings, x => x.RuleId == "core.image-alt");
        Assert.Contains(report.Coverage, x => x.State == CoverageState.Failed);
    }
    private sealed class Malformed(int kind) : ISeoCompanion
    {
        public Task<CompanionResult> AnalyzeAsync(SeoSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.FromResult(kind switch {
                0 => null!,
                1 => new CompanionResult(snapshot.Id, "bad", "1", CoverageState.Unavailable, default),
                _ => new CompanionResult(snapshot.Id, "bad", "1", CoverageState.Complete, [null!])
            });
    }
    [Fact] public async Task General_pages_do_not_call_companion()
    {
        var report = await analyzer.AnalyzeAsync(Prepare("<h1>Contact</h1><p>Call us.</p>"), new Never());
        Assert.Contains(report.Coverage, x => x.State == CoverageState.NotApplicable);
    }
    [Fact] public async Task Nonactionable_results_are_retained_without_becoming_advice()
    {
        var snapshot = Prepare("<h1>A</h1><p>Hello.</p>", ContentPurpose.Editorial);
        var report = await analyzer.AnalyzeAsync(snapshot, new Positive());
        Assert.DoesNotContain(report.Findings, x => x.Source == "positive");
        Assert.Single(report.CompanionDecisions);
        Assert.Equal(CoverageState.Complete, report.Coverage.Single(x => x.Provider == "positive").State);
    }
    [Fact] public async Task Cancellation_is_propagated()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => analyzer.Prepare(new("<p>A</p>"), cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => analyzer.AnalyzeAsync(Prepare("<h1>A</h1>"), cancellationToken: cts.Token));
    }
    private sealed class Never : ISeoCompanion
    {
        public Task<CompanionResult> AnalyzeAsync(SeoSnapshot snapshot, CancellationToken cancellationToken = default) => throw new Xunit.Sdk.XunitException("Companion must not run");
    }
    private sealed class Positive : ISeoCompanion
    {
        public Task<CompanionResult> AnalyzeAsync(SeoSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.FromResult(new CompanionResult(snapshot.Id, "positive", "1", CoverageState.Complete, [
                new("passive", "writing.passive-voice", FindingCategory.Readability, FindingSeverity.Information, "Looks good.", "")]));
    }
    private sealed class Fake(string id, CoverageState state) : ISeoCompanion
    {
        public Task<CompanionResult> AnalyzeAsync(SeoSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.FromResult(new CompanionResult(id, "test", "1", state, [
                new("passive", "writing.passive-voice", FindingCategory.Readability, FindingSeverity.Suggestion, "Use active voice.", "Rewrite."),
                new("sentence", "writing.sentence-length", FindingCategory.Readability, FindingSeverity.Suggestion, "Long.", "Split."),
                new("future", null, FindingCategory.Readability, FindingSeverity.Suggestion, "Unknown.", "Unknown.")]));
    }
}
