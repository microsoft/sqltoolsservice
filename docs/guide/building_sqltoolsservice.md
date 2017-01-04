## Build the SQL Tools Service

> SQL Tools Service depends on .Net Core SDK.  Please install .Net Core SDK before building.
> Please see [https://dotnet.github.io](https://dotnet.github.io/) for additional information on .Net Core.

1. Clone the SQL Tools Service repo from [https://github.com/Microsoft/sqltoolsservice](https://github.com/Microsoft/sqltoolsservice).
2. Run `dotnet restore` from the cloned repo's root directory.
3. Run `dotnet build src/Microsoft.SqlToosl.ServiceLayer` from the cloned repo's root directory.

> *Tip* there is a `build.cmd` or `build.sh` file in the repo's root directory that can be used 
> to build and package the service.

## Building the Documentation

> The documentation is generated using docfx.  Please install docfx from 
> [https://dotnet.github.io/docfx/](https://dotnet.github.io/docfx/).

1. Clone the SQL Tools Service docs repo from [https://github.com/Microsoft/sqltoolssdk](https://github.com/Microsoft/sqltoolssdk).
2. Run `docfx docfx.json --serve` from the docs directory.
3. Copy the contents of the docs/_site directory to the root directory of the sqltoolssdk repo.

## Run Tests

The SQL Tools Service has several different types of tests such as unit tests, "connected" tests, 
integration tests, perf tests and stress tests. Additionally, there is also test configuration 
scripts to collect code coverage results.

### Running Unit Tests

1. Run `dotnet restore` from the cloned repo's root directory.
2. Run `dotnet test test/Microsoft.SqlToosl.ServiceLayer.Test` from the cloned repo's root directory.

The test output should be similar to the below.  There may also be additional debugging output based on 
test details and execution environment.

```
xUnit.net .NET CLI test runner (64-bit win10-x64)
  Discovering: Microsoft.SqlTools.ServiceLayer.Test
  Discovered:  Microsoft.SqlTools.ServiceLayer.Test
  Starting:    Microsoft.SqlTools.ServiceLayer.Test
=== TEST EXECUTION SUMMARY ===
   Microsoft.SqlTools.ServiceLayer.Test  Total: 434, Errors: 0, Failed: 0, Skipped: 0, Time: 21.139s
SUMMARY: Total: 1 targets, Passed: 1, Failed: 0.
```

### Collecting Code Coverage

> Code coverage requires a SQL Server instance installed on the localhost with Integrated Authentication 
> configured for the current user executing the test suite.

> Code coverage can only be collected from Windows at this time.

1. Run `npm install` from test/CodeCoverage directory.
2. Run `runintegration.cmd` from test/CodeCoverage directory.
3. Code coverage results will be available in the test/CodeCoverage/reports/index.html report.
