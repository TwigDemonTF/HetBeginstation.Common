# HetBeginstation.Common
A shared C# utility library distributed through GitHub Packages.

## Installation
1. Configure the NuGet source

Add GitHub Packages as a NuGet source. Replace YOUR-USERNAME with the GitHub account that owns the package.

```fish
dotnet nuget add source \
    "https://nuget.pkg.github.com/YOUR-USERNAME/index.json" \
    --name github \
    --username YOUR-USERNAME \
    --password "$GITHUB_PACKAGES_TOKEN" \
    --store-password-in-clear-text
```

Your GITHUB_PACKAGES_TOKEN must have the appropriate read:packages permission. Keep your token secure and never commit it to source control.

2. Install the package
In your .NET project directory, run:

```bash
dotnet add package HetBeginstation.Common --version 1.0.0
```

Replace the version with the release you want to use.

## Usage
Import the library's namespace:

```csharp
using HetBeginstation.Common;
```

Example:

```csharp
bool result =
    StringExtensions.IsNullOrEmptyOrWhiteSpace("hello");
```
## Updating the package
When a new version is released, update your project to that version:

```bash
dotnet add package HetBeginstation.Common --version 1.0.1
dotnet restore
```
## Publishing a new version
Make your code changes.
Increment <Version> in MyCompany.Common.csproj, for example from 1.0.0 to 1.0.1.
Pack the project:
```bash
dotnet pack --configuration Release
```
Publish the new package using Fish:
```fish
dotnet nuget push \
    "bin/Release/HetBeginstation.Common.1.0.1.nupkg" \
    --source github \
    --api-key "$GITHUB_PACKAGES_TOKEN"
```

Update the filename to match the version being published.
Each release must have a unique version. Previous versions remain available for projects that still depend on them.

## Requirements
- .NET SDK compatible with the project.
- Access to the private GitHub Packages feed.
- A GitHub personal access token with the necessary package permissions.

Note: Replace YOUR-USERNAME and the example versions with the appropriate values for your setup.