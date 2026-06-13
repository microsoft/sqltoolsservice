# STS2 client interop

STS2 speaks JSON-RPC 2.0 over Content-Length framing on the same stdio pair as the
legacy service. A client enables STS2 by launching the service exe with `--enable-sts2`
(or `STS_ENABLE_STS2=1`) and addressing v2 methods by their `v2/` prefix; everything else
routes to the legacy service unchanged. See `docs/sts2/CONTRACT.md` for the full method
table and `docs/sts2/TRACE-SCHEMA.md` for the envelope/trace format.

## Lifecycle

1. `v2/initialize` — handshake; returns capabilities, limits, drivers, and the current
   capture summary. Idempotent.
2. `v2/connection.open` — open a connection (client-generated `openId`); returns
   `connectionId`. `v2/connection.cancel` (by `openId`) and `v2/connection.close` (by
   `connectionId`) are idempotent.
3. `v2/query.execute` — returns `queryId` immediately; results arrive as notifications:
   `v2/query.resultSet`, `v2/query.rows`, `v2/query.message`, then exactly one
   `v2/query.complete`.
4. Backpressure: the server streams up to `windowPages` unacked pages; the client sends
   `v2/query.ack` (per-page `pageSeq` or high-water `throughPageSeq`) to open the window.
5. `v2/query.cancel` and `v2/query.dispose` are idempotent. `v2/diagnostics.{ping,health,
   state,exportLog}` are always available.

## TypeScript sample (vscode-jsonrpc)

This is a copy-pasteable reference; it is documented rather than run in CI (no Node
toolchain is assumed in this repo). The wire shapes match `docs/sts2/CONTRACT.md`.

```typescript
import { spawn } from "node:child_process";
import {
  createMessageConnection,
  StreamMessageReader,
  StreamMessageWriter,
} from "vscode-jsonrpc/node";

const proc = spawn("MicrosoftSqlToolsServiceLayer", ["--enable-sts2"], { stdio: "pipe" });
const conn = createMessageConnection(
  new StreamMessageReader(proc.stdout),
  new StreamMessageWriter(proc.stdin),
);

// Streaming notifications.
const pages: unknown[][] = [];
conn.onNotification("v2/query.resultSet", (p: any) => console.log("columns:", p.columns));
conn.onNotification("v2/query.rows", (p: any) => {
  pages.push(...p.rows);
  // Acknowledge to keep the window open (high-water form).
  conn.sendNotification("v2/query.ack", { queryId: p.queryId, throughPageSeq: p.pageSeq });
});

const completion = new Promise<any>((resolve) =>
  conn.onNotification("v2/query.complete", resolve),
);

conn.listen();

await conn.sendRequest("v2/initialize", { clientName: "sample", requestedSpecVersion: "2.0" });

const { connectionId } = await conn.sendRequest("v2/connection.open", {
  openId: "open-1",
  profile: {
    server: ":memory:",
    driver: "sqlite",
    auth: { kind: "integrated" },
  },
});

await conn.sendRequest("v2/query.execute", { connectionId, sql: "create table t(n integer)" });
// ... await each query.complete before the next execute (one active query per connection).

const { queryId } = await conn.sendRequest("v2/query.execute", {
  connectionId,
  sql: "select n from t order by n",
});
const result = await completion; // { queryId, status: "succeeded", rowsAffected }
console.log("rows:", pages.length, "status:", result.status);

await conn.sendRequest("v2/connection.close", { connectionId });
```

## Errors

JSON-RPC `error.code` is numeric; the stable identity is `error.data.code` (an `Sts2.*`
string — see `docs/sts2/CONTRACT.md`). Raw exception strings are never contract.

## Export bundles

`v2/diagnostics.exportLog` writes a shareable zip (`sts2-export-<runId>.zip`) with a
manifest, a privacy report (canary-scan result), the journal segments, and the generated
review docs. In product capture mode the bundle contains no SQL text, row cells, or
secrets. Validate a bundle with `sts2-replay export-check <bundle.zip>`.
