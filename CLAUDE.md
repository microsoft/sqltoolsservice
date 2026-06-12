# SqlToolsService

## STS2 branch rules

- Contract: `docs/sts2/SPEC.md`. Operating rules: `docs/sts2/AGENT-RUNBOOK.md`.
- Definition of done: `./verify.sh --quick` green plus a report entry.
- Human gates: end of M1 and end of M7 only, unless a Stop Condition triggers.
- Never weaken, skip, delete, or retry tests to go green.
- Never edit golden/scenario output to match broken behavior.
- Never touch legacy code outside the SPEC §5 seam.
- Never log secrets, SQL text in product default, row payloads in digest mode, or unframed stdout text.
- Core is pure: no time, randomness, async, channels, driver APIs, ADO.NET, StreamJsonRpc, file I/O, console I/O, or legacy namespaces.
- Cancellation is a journaled v2 message, never hidden transport cancellation.
- Multiplexer must rewrite outbound server-request ids; a plain id-to-channel table is not enough.
- `shutdown` and `exit` are lifecycle-mirrored to STS2, not raw-broadcast as duplicate JSON-RPC requests.
- One-way doors require `SPEC-CHANGE` in `docs/sts2/DECISIONS.md` and a stop.
