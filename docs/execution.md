# SEO analysis packages — execution

Approved plan: headless MIT .NET core; optional server-side Yoast HTTP client and separately deployed container. Packages and sample qualification precede Puppies Plus integration.

Work happens on feature/initial-analysis-packages in the existing sibling checkouts; no worktrees. The owner already renamed the optional repository.

## Tasks

- Repository scaffolding and contract tests.
- Core document preparation, baseline checks and companion policy.
- Optional client, bounded Yoast service, integration tests.
- Packaged console / Blazor Server / WebAssembly consumers.
- Qualification, source distribution, CI and final review.

## Rulings

- The approved in-chat plan is the specification; this record persists its implementation decisions.
- The service owns GPL Yoast code. Original C# client code is MIT; no GPL assets are included in its NuGet package. This is an engineering boundary, not a legal guarantee for downstream deployments.
- Server-only service access replaces the experiment's browser worker. Browser execution remains a future licensing-reviewed experiment.

## Evidence and completion

- Scaffold and contracts complete. Initial13 behavior checks failed on the unimplemented methods, then passed with preparation, baseline rules and policy.
- Positive companion assessments reproduced as unwanted advice before correction; retained as diagnostics after correction.
- Final Windows/Linux:19 core tests each, including all seven controlled correction pairs in24 owned fixtures. Packaged core-only console passes on both platforms.
- Real optional companion qualification:34 documents, all baseline findings retained,136 degraded checks;40 core findings plus10 companion actions. The earlier experiment's36 core findings are not treated as a frozen parser specification. HTML5 normalization is now owned by this package.
- Final independent review found an actual pre-fix stack overflow at495,004 characters, below the input size cap. A1000-level nesting test failed RED; a256-level iterative guard before traversal/serialization fixes it GREEN. Three malformed companion shapes similarly reproduced NullReference failures RED and now preserve baseline with Failed coverage.
- Review has no unresolved Critical/Important findings. Its two Minor findings in the sibling client/CI were also addressed.
- Qualification packages use temporary local0.1.0-local.4 versions. GitVersion still controls actual releases; no version was published or pushed.

The sibling Yoast repository contains detailed qualification.json, scripts and measured transport/browser results. No production Puppies Plus files changed.

## Limits

English writing heuristics only; metadata/structure remain available for other locales. Applicability follows explicit editorial purpose. No ranking, linguistic accuracy, owner UAT or real-phone claim is made. GitHub-hosted publishing has not run. The core must be released before normal restore of its dependent sibling.
