# CI bootstrap

GitHub App token used by Arena currently cannot push files under `.github/workflows/`
without the `workflows` permission.

## Option A — add workflow in GitHub UI

1. Open the branch `arena/019f9a82-igbz-pro` on GitHub.
2. Create file: `.github/workflows/ci.yml`
3. Paste the contents of `github-actions-build-test.yml` from this folder.
4. Commit on the same branch.
5. Confirm the `build-test` workflow run is green.

## Option B — local build

```bash
dotnet restore IGBZ.sln
dotnet build IGBZ.sln --configuration Release
dotnet test IGBZ.sln --configuration Release
```

Paste the full console output back into Arena if anything fails.
