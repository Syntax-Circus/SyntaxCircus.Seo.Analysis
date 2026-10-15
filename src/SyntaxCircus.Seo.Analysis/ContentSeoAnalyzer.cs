using System.Collections.Immutable;

namespace SyntaxCircus.Seo.Analysis;

public sealed class ContentSeoAnalyzer : IContentSeoAnalyzer
{
    public SeoSnapshot Prepare(SeoDocumentInput input, CancellationToken cancellationToken = default)
        => DocumentPreparation.Prepare(input, cancellationToken);

    public async Task<SeoAnalysisReport> AnalyzeAsync(SeoSnapshot snapshot, ISeoCompanion? companion = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        var findings = BaselineRules.Analyze(snapshot).ToBuilder();
        var coverage = ImmutableArray.CreateBuilder<AnalysisCoverage>();
        var decisions = ImmutableArray.CreateBuilder<AssessmentDecision>();
        var english = BaselineRules.English(snapshot.Locale);
        coverage.Add(new("core.structure", CoverageState.Complete));
        coverage.Add(new("core.writing", english ? CoverageState.Complete : CoverageState.UnsupportedLanguage));
        if (companion is null) coverage.Add(new("companion", CoverageState.NotRequested));
        else if (!english) coverage.Add(new("companion", CoverageState.UnsupportedLanguage));
        else if (snapshot.Purpose != ContentPurpose.Editorial) coverage.Add(new("companion", CoverageState.NotApplicable, "Deeper writing checks require editorial purpose."));
        else
        {
            CompanionResult result;
            try { result = await companion.AnalyzeAsync(snapshot, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (OperationCanceledException) { result = new(snapshot.Id, "companion", "", CoverageState.TimedOut, []); }
            catch (Exception) { result = new(snapshot.Id, "companion", "", CoverageState.Failed, [], "Companion analysis failed."); }
            cancellationToken.ThrowIfCancellationRequested();
            if (result is null || result.Assessments.IsDefault || result.Assessments.Any(x => x is null))
                result = new(snapshot.Id, "companion", "", CoverageState.Failed, [], "Malformed companion result.");
            var state = result.SnapshotId != snapshot.Id ? CoverageState.Stale : result.State;
            var usable = state is CoverageState.Complete or CoverageState.Partial;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in result.Assessments)
            {
                var reason = !usable ? "Companion coverage unavailable"
                    : item.SuppressionReason is not null ? item.SuppressionReason
                    : item.SemanticKey is null ? "Unrecognized assessment"
                    : BaselineRules.OwnedKeys.Contains(item.SemanticKey) ? "Owned by core"
                    : item.Severity == FindingSeverity.Information ? "No action suggested"
                    : item.RequiresFocusPhrase && snapshot.FocusPhrase is null ? "No focus phrase selected"
                    : !keys.Add(item.SemanticKey) ? "Duplicate companion finding"
                    : "Included";
                decisions.Add(new(item, reason == "Included", reason));
                if (reason == "Included") findings.Add(new(result.Provider + "." + item.RawId, item.SemanticKey!, item.Category,
                    item.Severity, item.Message, item.Recommendation, result.Provider));
            }
            if (usable && result.Assessments.Any(x => x.SemanticKey is null && x.SuppressionReason is null)) state = CoverageState.Partial;
            coverage.Add(new(result.Provider, state, result.Detail, result.EngineVersion));
        }
        return new(snapshot.Id, findings.ToImmutable(), coverage.ToImmutable(), decisions.ToImmutable());
    }
}
