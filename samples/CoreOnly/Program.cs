using SyntaxCircus.Seo.Analysis;
var analyzer = new ContentSeoAnalyzer();
var snapshot = analyzer.Prepare(new("<h1>Our dogs</h1><section data-section-id='photos'><img src='/dog.jpg'></section>", "Our dogs", "Meet our family dogs."));
var report = await analyzer.AnalyzeAsync(snapshot);
foreach (var finding in report.Findings) Console.WriteLine($"{finding.SectionId}: {finding.Message}");
if (report.Findings.Single().RuleId != "core.image-alt") throw new Exception("Unexpected core-only report.");
Console.WriteLine("Core-only package consumer passed.");
