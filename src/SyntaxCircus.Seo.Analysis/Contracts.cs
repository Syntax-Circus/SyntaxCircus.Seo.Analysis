using System.Collections.Immutable;

namespace SyntaxCircus.Seo.Analysis;

public enum ContentPurpose { General, Editorial }
public enum FindingCategory { Metadata, Structure, FocusPhrase, Readability }
public enum FindingSeverity { Information, Suggestion, Warning }
public enum CoverageState { Complete, Partial, NotRequested, NotApplicable, UnsupportedLanguage, Unavailable, Failed, TimedOut, Stale }
public sealed record SeoDocumentInput(string ContentHtml, string Title = "", string MetaDescription = "",
    string CanonicalUrl = "", string Slug = "", string? FocusPhrase = null,
    string Locale = "en_US", ContentPurpose Purpose = ContentPurpose.General);
public sealed record SeoHeading(int Level, string Text, string? SectionId);
public sealed record SeoImage(string? Alt, string? SectionId);
public sealed record SeoParagraph(string Text, string? SectionId);
public sealed record SeoSnapshot(string Id, string Title, string MetaDescription, string CanonicalUrl,
    string Slug, string? FocusPhrase, string Locale, ContentPurpose Purpose, string ContentHtml,
    string ReadabilityHtml, string Prose, ImmutableArray<SeoHeading> Headings,
    ImmutableArray<SeoImage> Images, ImmutableArray<SeoParagraph> Paragraphs);
public sealed record SeoFinding(string RuleId, string SemanticKey, FindingCategory Category,
    FindingSeverity Severity, string Message, string Recommendation, string Source = "core",
    string? SectionId = null);
public sealed record AnalysisCoverage(string Provider, CoverageState State, string? Detail = null, string? EngineVersion = null);
public sealed record CompanionAssessment(string RawId, string? SemanticKey, FindingCategory Category,
    FindingSeverity Severity, string Message, string Recommendation, bool RequiresFocusPhrase = false,
    double? Score = null, string? SuppressionReason = null);
public sealed record CompanionResult(string SnapshotId, string Provider, string EngineVersion,
    CoverageState State, ImmutableArray<CompanionAssessment> Assessments, string? Detail = null);
public sealed record AssessmentDecision(CompanionAssessment Assessment, bool Included, string Reason);
public sealed record SeoAnalysisReport(string SnapshotId, ImmutableArray<SeoFinding> Findings,
    ImmutableArray<AnalysisCoverage> Coverage, ImmutableArray<AssessmentDecision> CompanionDecisions);
public interface ISeoCompanion
{
    Task<CompanionResult> AnalyzeAsync(SeoSnapshot snapshot, CancellationToken cancellationToken = default);
}
public interface IContentSeoAnalyzer
{
    SeoSnapshot Prepare(SeoDocumentInput input, CancellationToken cancellationToken = default);
    Task<SeoAnalysisReport> AnalyzeAsync(SeoSnapshot snapshot, ISeoCompanion? companion = null,
        CancellationToken cancellationToken = default);
}
