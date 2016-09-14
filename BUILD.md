# Build

To build the vscode-mssql Service Layer, the most straight-forward way is open the source project 
(./src/Microsoft.SqlTools.ServiceLayer/ folder), restore the project's NuGet dependencies (which
is done automatically in Visual Studio), and run the build task in VSCode or Visual Studio
(<kbd>Ctrl</kbd><kbd>Shift</kbd><kbd>B</kbd>). This will compile the code and drop everything in
the ./bin/ folder off of the root of the project. This is the most simple way and will produce the
dll files you can run using the `dotnet` command.

If you need to do something special, read on for details about Cake.

# Cake

In addition to the standard build process, we have added a much more robust build system for
performing complicated build processes that require more than just compiling the code. As such
Cake ([cakebuild.net](http://www.cakebuild.net/)) was selected for the task. Running cake is only
necessary in the following scenarios

* Regenerating resource files after adding a new string
* Packaging the Service Layer into a NuGet package

## Cake Prerequisites

Cake requires Mono to be installed on non-Windows machines as Cake is not built using .NET Core (yet).

## Cake Usage

Run `build.(cmd|ps1|sh)` with the desired set of arguments (see below for options).
The build script itself is `build.cake`, written in Cake's C#-like DSL using the Cake build automation system.

## Arguments

### Primary

  `-target=TargetName`: The name of the build task/target to execute (see below for listing and details).
    Defaults to `Default`.

  `-configuration=(Release|Debug)`: The configuration to build.
    Defaults to `Release`.

### Extra

  `-test-configuration=(Release|Debug)`: The configuration to use for the unit tests.
    Defaults to `Debug`.
  
  `-install-path=Path`: Path used for the **Install** target.
    Defaults to `(%USERPROFILE%|$HOME)/.sqltoolsservice/local`
  
  `-archive`: Enable the generation of publishable archives after a build.

## Targets

**Default**: Alias for Local.

**Local**: Full build including testing for the machine-local runtime.

**All**: Same as local, but targeting all runtimes selected by `PopulateRuntimes` in `build.cake`.
  Currently configured to also build for a 32-bit Windows runtime on Windows machines.
  No additional runtimes are currently selected on non-Windows machines.

**Quick**: Local build which skips all testing.

**Install**: Same as quick, but installs the generated binaries into `install-path`.

**SetPackageVersions**: Updates the dependency versions found within `project.json` files using information from `depversion.json`.
  Used for maintainence within the project, not needed for end-users. More information below.

**SRGen**: Generates a new version of the `sr.resx`, `sr.cs`, and `sr.designer.cs` files that contain
  the string resources defined in `sr.strings`. Run this after adding a new string to `sr.strings`

## Cake Configuration files

### build.json

A number of build-related options, including folder names for different entities. Interesting options:

**DotNetInstallScriptURL**: The URL where the .NET SDK install script is located.
  Can be used to pin to a specific script version, if a breaking change occurs.
  
**"DotNetChannel"**: The .NET SDK channel used for retreiving the tools.

**"DotNetVersion"**: The .NET SDK version used for the build. Can be used to pin to a specific version.
  Using the string `Latest` will retrieve the latest version.

### depversion.json

A listing of all dependencies (and their desired versions) used by `project.json` files throughout the project.
Allows for quick and automatic updates to the dependency version numbers using the **SetPackageVersions** target.

## Artifacts Generated From Cake

* Binaries of Microsoft.SqlTools.ServiceLayer and its libraries built for the local machine in `artifacts/publish/Microsoft.SqlTools.ServiceLayer/default/{framework}/`
* Scripts to run Microsoft.SqlTools.ServiceLayer at `scripts/SQLTOOLSSERVICE(.Core)(.cmd)`
  * These scripts are updated for every build and every install.
  * The scripts point to the installed binary after and install, otherwise just the build folder (reset if a new build occurs without an install).
* Binaries of Microsoft.SqlTools.ServiceLayer and its libraries cross-compiled for other runtimes (if selected in **PopulateRuntimes**) `artifacts/publish/Microsoft.SqlTools.ServiceLayer/{runtime}/{framework}/`
* Test logs in `artifacts/logs`
* Archived binaries in `artifacts/package` (only if `-archive` used on command line)
