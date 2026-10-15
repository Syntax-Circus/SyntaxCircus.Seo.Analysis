# SyntaxCircus.Seo.Analysis

Headless .NET 10 analysis for content authors. MIT licensed. No ASP.NET, JavaScript, database, AI service, or other SyntaxCircus runtime dependency. HTML preparation uses AngleSharp 1.8.5.

```csharp
var analyzer = new ContentSeoAnalyzer();
var snapshot = analyzer.Prepare(new SeoDocumentInput(
    ContentHtml: "<h1>Puppy care</h1><p>Prepare a quiet resting place.</p>",
    Title: "Puppy care",
    MetaDescription: "Help your puppy settle at home.",
    Purpose: ContentPurpose.Editorial));
var report = await analyzer.AnalyzeAsync(snapshot, cancellationToken: token);
```

## Input and findings

Supply the content region of the effective page: include a layout-provided H1 if it belongs to that page. The analyzer does not crawl URLs, execute scripts, sanitize content for display, or infer CSS visibility. Never render supplied HTML simply because it has been analyzed.

Use data-section-id on sections to locate structural/image/paragraph findings. Excluded from all analysis: script, style, nav, footer, hidden, aria-hidden=true, data-seo-exclude, data-unplaced, data-inactive-theme and data-seo-scope=chrome. Code/pre, data-generated and data-seo-prose=false remain structural content but are excluded from editorial prose. Visible HTML is normalized using an HTML5 parser, without network loaders.

Prepare returns an immutable snapshot and a deterministic content/metadata identity. A changed title, phrase, purpose or body produces a new identity. The host still owns debounce, cancellation and rejecting responses for an older editor generation. Maximum HTML input is 500,000 characters; parsed nesting is limited to256 levels before recursive traversal or serialization.

Checks cover title/description presence, one H1 and heading order, missing alt, optional chosen-phrase presence/density, long sentences/paragraphs and immediate word repetition. Empty alt is allowed for decorative images. English prose checks are heuristic; other languages retain metadata/structure coverage and explicitly report the writing gap.

Length defaults: title >65 and description >160 characters produce information, not failure; sentences >25 words in >25% of sentences and paragraphs >150 words produce suggestions. Phrase frequency >5% requires at least30 words. No minimum word/link/image quota is imposed. These defaults are authoring hints, not search-engine limits or ranking predictions.

## Companions

ISeoCompanion receives the same prepared snapshot. Pass it to AnalyzeAsync to add provider-specific writing assistance. Core-owned semantic keys take precedence even when the core produces no finding. Reports retain every companion assessment, raw score, inclusion/suppression reason and provider version, without averaging scores.

Only ContentPurpose.Editorial enables deeper companion checks. Hosts opt articles and other editorial pages in explicitly. Null companions, non-editorial pages, unsupported languages, failed calls, timeouts and stale identities have explicit coverage states. An unavailable companion cannot remove baseline findings. Information-only companion assessments stay diagnostic; they are not actions.

## Development

Run dotnet test -c Release. Fixtures contain24 owned synthetic inputs and seven controlled correction pairs; no network or sibling checkout is needed for unit tests. samples/CoreOnly consumes an actual NuGet package rather than the source project. Its SeoAnalysisVersion defaults to0.1.0 and can be overridden for a temporary local feed.

Versions follow GitVersion. Feature branches build/test; main pushes publish through the existing release environment and NuGet trusted publishing. Qualification uses an explicit temporary version only for local package-consumer checks. Publish the core before releasing its dependent Yoast client.

See docs/execution.md for measured qualification and limits.
