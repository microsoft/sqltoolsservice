# SQLTools Async Binding Queue Migration Plan

## Objective

Make SQLTools queue-driven language APIs non-blocking and timeout-safe across:
- `LanguageServices`
- `ObjectExplorer`
- `FileBrowser`

Keep per-connection serialization after timeout: caller can receive timeout quickly, but the next operation for the same connection should not start until the timed-out operation exits.

## Scope

Included:
- `src/Microsoft.SqlTools.ServiceLayer/LanguageServices`
- `src/Microsoft.SqlTools.ServiceLayer/ObjectExplorer`
- `src/Microsoft.SqlTools.ServiceLayer/FileBrowser`

Excluded:
- `src/Microsoft.Kusto.ServiceLayer`

## Phased Plan

1. Queue foundation
- Add awaitable queue result contract to `QueueItem`.
- Add `QueueBindingOperationAsync(...)` API to `BindingQueue`.
- Keep existing sync queue API temporarily for compatibility.

2. Timeout correctness and non-blocking queue internals
- Remove blocking post-timeout wait (`bindTask.Wait()`).
- Ensure timeout result is surfaced to caller promptly.
- Keep lock release serialized by releasing binding lock only after timed-out operation exits.

3. Language service migration
- Replace `ItemProcessed.WaitOne()` usage with awaitable queue calls in language service flow.
- Remove sync-over-async (`GetAwaiter().GetResult`) in in-scope request paths.

4. Object explorer migration
- Replace queue `WaitOne()` waits in session creation and expand flows with awaitable queue calls.

5. File browser migration
- Replace queue `WaitOne()` waits in open/expand/validate/close flows with awaitable queue calls.

6. Verification and regressions
- Build and run focused unit/integration tests.
- Add/adjust timeout tests for non-blocking behavior.

## Verification Commands

```bash
dotnet build /Users/aasim/src/sqltoolsservice/sqltoolsservice.sln

dotnet test /Users/aasim/src/sqltoolsservice/test/Microsoft.SqlTools.ServiceLayer.UnitTests/Microsoft.SqlTools.ServiceLayer.UnitTests.csproj --filter "FullyQualifiedName~BindingQueueTests|FullyQualifiedName~CompletionServiceTest|FullyQualifiedName~AutocompleteTests|FullyQualifiedName~LanguageServiceTests|FullyQualifiedName~ObjectExplorerServiceTests"

dotnet test /Users/aasim/src/sqltoolsservice/test/Microsoft.SqlTools.ServiceLayer.IntegrationTests/Microsoft.SqlTools.ServiceLayer.IntegrationTests.csproj --filter "FullyQualifiedName~FileBrowserServiceTests|FullyQualifiedName~ObjectExplorerServiceTests|FullyQualifiedName~LanguageServiceTests"
```

## Notes

- Keep request handler contracts unchanged.
- Internal method signatures can become async.
- Preserve existing unrelated local changes.
