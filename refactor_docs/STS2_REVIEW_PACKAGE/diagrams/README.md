# STS2 Design Visuals v2

The visual companion contains 12 landscape pages:

1. Cover and legend.
2. System context and trust boundaries.
3. Ownership and orderly shutdown.
4. One pump turn and the durability barrier.
5. Fatal containment and pending-request drain.
6. Connection and query lifecycle.
7. Backpressure at the real driver edge.
8. Run-scoped journal and strict replay.
9. Privacy by policy and scoped restoration.
10. Observer isolation and exact viewer resynchronization.
11. Coherent export and verification.
12. Release evidence and adoption ladder.

The diagrams retain the original pastel component-language idea while adding explicit ownership, trust, durability, failure, privacy, and release-evidence semantics.

## Build

From this directory:

```bash
pdflatex -interaction=nonstopmode -halt-on-error STS2_DESIGN_VISUALS_V2.tex
pdflatex -interaction=nonstopmode -halt-on-error STS2_DESIGN_VISUALS_V2.tex
```

The source uses standard LaTeX packages including TikZ and `adjustbox`. The checked-in PDF is the reviewed output.
