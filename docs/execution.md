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

## Evidence

Pending.
