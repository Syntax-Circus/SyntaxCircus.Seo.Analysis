using System.Text.Json;
using SyntaxCircus.Seo.Analysis;
using Xunit;
public sealed class CorpusTests
{
    [Fact] public async Task Seven_controlled_corrections_reduce_the_targeted_core_issue()
    {
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(await File.ReadAllTextAsync("owned.json", TestContext.Current.CancellationToken), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var pairs = new[] { ("description", "core.description"), ("headings", "core.heading-order"), ("image", "core.image-alt"), ("phrase", "core.phrase-title"), ("stuffing", "core.phrase-density"), ("repeat", "core.repeated-words"), ("long", "core.sentence-length") };
        var analyzer = new ContentSeoAnalyzer();
        foreach (var (id, rule) in pairs)
        {
            async Task<int> Count(string suffix)
            {
                var input = fixtures.Single(x => x.Id == "truth-" + id + "-" + suffix).Input;
                var report = await analyzer.AnalyzeAsync(analyzer.Prepare(input, TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken);
                return report.Findings.Count(x => x.RuleId == rule);
            }
            Assert.True(await Count("bad") > await Count("good"), id);
        }
    }
    private sealed record Fixture(string Id, SeoDocumentInput Input);
}
