# STS2 Review Package

This package contains a detailed technical review, a rebuilt target design, a reliability/privacy/operations companion, an implementation plan, a structured findings register, and a redesigned set of architecture diagrams for the SQL Tools Service STS2 refactor.

## Start here

1. Read [`00_EXECUTIVE_SUMMARY.md`](00_EXECUTIVE_SUMMARY.md).
2. Use [`01_TECHNICAL_REVIEW.md`](01_TECHNICAL_REVIEW.md) for code-level findings and release disposition.
3. Use [`03_TARGET_DESIGN.md`](03_TARGET_DESIGN.md) and [`04_RELIABILITY_SECURITY_OPERATIONS.md`](04_RELIABILITY_SECURITY_OPERATIONS.md) as the proposed design baseline.
4. Execute [`05_NEXT_STEPS.md`](05_NEXT_STEPS.md) in order.
5. Track closure in [`06_FINDINGS_REGISTER.csv`](06_FINDINGS_REGISTER.csv).
6. Review or present [`diagrams/STS2_DESIGN_VISUALS_V2.pdf`](diagrams/STS2_DESIGN_VISUALS_V2.pdf).

## Package map

| File | Purpose |
|---|---|
| `00_EXECUTIVE_SUMMARY.md` | Decision-level assessment, top blockers, and recommended sequence. |
| `01_TECHNICAL_REVIEW.md` | Detailed review of architecture, implementation, tests, workflow, and branch delta. |
| `02_DOCUMENTATION_REVIEW.md` | Review of every supplied document and proposed authority/status structure. |
| `03_TARGET_DESIGN.md` | Full target architecture and contracts after incorporating review findings. |
| `04_RELIABILITY_SECURITY_OPERATIONS.md` | Fatal model, ownership, durability, privacy, observability, SLOs, rollout, and support. |
| `05_NEXT_STEPS.md` | Concrete waves, tasks, target files, tests, and completion gates. |
| `06_FINDINGS_REGISTER.csv` | 50 findings with severity, evidence, impact, recommendation, and validation. |
| `diagrams/STS2_DESIGN_VISUALS_V2.pdf` | 12-page visual design companion. |
| `diagrams/STS2_DESIGN_VISUALS_V2.tex` | Editable TikZ source for the visual companion. |
| `diagrams/README.md` | Diagram index and build instructions. |

## Review basis

The review covered:

- all supplied STS2 Markdown, TeX, and PDF artifacts;
- the accessible `microsoft/sqltoolsservice` branch `sts2/main` at commit `c9fbd1e40ec8aae43f02bd31723f2fa205d8d849`;
- its pending changes relative to `main`;
- product projects, multiplexer/bootstrap seam, runtime, core, drivers, replay/export tooling, tests, scripts, workflow, and generated docs.

The branch was inspected read-only. The package does not claim that the branch was independently compiled or executed in this environment.

## Finding severity

- **Blocker:** can violate request termination, durability, replay truth, privacy, or merge-candidate evidence.
- **High:** likely correctness, resource, security, or operability defect that should be fixed before preview.
- **Medium:** important contract, performance, diagnostics, maintainability, or documentation gap.
- **Low:** polish or future-proofing work with limited immediate risk.

## Recommended disposition

Preserve the architectural direction, but complete the hardening waves before a preview tag or merge to `main`. The most important design correction is to make session lifetime, pump barriers, exact-run replay, and resource ownership first-class protocols rather than conventions.
