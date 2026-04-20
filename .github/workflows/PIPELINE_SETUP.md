# GitHub Actions Pipeline Setup Guide

This guide explains how the CI/CD pipeline works and what steps you need to take to get it running in your GitHub repository.

---

## What the pipeline does

The pipeline (`ci-cd.yml`) mirrors your existing Azure DevOps pipeline and has two jobs:

| Job | When does it run? | What does it do? |
|---|---|---|
| **Build and Unit Test** | Every push to `main` and every pull request | Restores, builds, runs unit tests, collects code coverage |
| **Package and Publish** | Only when a version tag is pushed (e.g. `v1.1.42`) | Packs NuGet packages, publishes to GitHub Packages, creates a GitHub Release |

### Packages that get published

| NuGet Package | Source project |
|---|---|
| `LogicApps.Management` | `Management/LogicApps.Management.csproj` |
| `LogicApps.TestFramework.Specifications` | `TestFramework.Specifications/LogicApps.TestFramework.Specifications.csproj` |

---

## Files created

```
.github/
├── workflows/
│   └── ci-cd.yml                   ← The GitHub Actions pipeline
└── scripts/
    └── set-package-version.ps1     ← Versioning script (GitHub equivalent of set.package.version.ps1)
```

---

## Step 1 — Push the files to GitHub

Commit and push the new `.github` folder to `main`:

```powershell
git add .github
git commit -m "Add GitHub Actions CI/CD pipeline"
git push origin main
```

This triggers the pipeline immediately. Only the **Build and Unit Test** job will run at this point.

---

## Step 2 — Enable Actions write permissions

This is required so the pipeline can publish NuGet packages and create GitHub Releases.

1. Go to your repository on GitHub: `https://github.com/sahinozd/LogicAppsStandard.Testing`
2. Click **Settings** (top menu)
3. In the left sidebar click **Actions** → **General**
4. Scroll down to **Workflow permissions**
5. Select **Read and write permissions**
6. Click **Save**

---

## Step 3 — Trigger a full release (Build + Test + Package)

Packaging only runs when you push a version tag. Tags follow the format `vMAJOR.MINOR.PATCH`.

```powershell
# Example: release version 1.1.0
git tag v1.1.0
git push origin v1.1.0
```

After pushing the tag, GitHub Actions will:

1. ✅ Build the solution
2. ✅ Run all unit tests
3. ✅ Pack `LogicApps.Management` and `LogicApps.TestFramework.Specifications` as version `1.1.0`
4. ✅ Publish both packages to **GitHub Packages** (visible under your repo's **Packages** tab)
5. ✅ Create a **GitHub Release** (visible under your repo's **Releases** tab) with the `.nupkg` files attached

---

## Step 4 (Optional) — Also publish to NuGet.org

If you want packages to be publicly available on [nuget.org](https://www.nuget.org):

### 4a — Create a NuGet.org API key

1. Sign in at [nuget.org](https://www.nuget.org)
2. Click your username → **API Keys**
3. Click **Create** and give it a name (e.g. `GitHub Actions`)
4. Copy the generated key

### 4b — Add the key as a GitHub secret

1. Go to your repo on GitHub
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Set **Name** to `NUGET_API_KEY`
5. Paste your NuGet.org key as the **Value**
6. Click **Add secret**

### 4c — Uncomment the publish step in the pipeline

In `.github/workflows/ci-cd.yml`, find the commented-out block and uncomment it:

```yaml
- name: Publish to NuGet.org
  run: |
    dotnet nuget push "./artifacts/**/*.nupkg" \
      --source https://api.nuget.org/v3/index.json \
      --api-key ${{ secrets.NUGET_API_KEY }} \
      --skip-duplicate
```

---

## Versioning

Versions are driven by Git tags:

| Tag pushed | Package version |
|---|---|
| `v1.1.0` | `1.1.0` |
| `v1.2.5` | `1.2.5` |
| `v2.0.0` | `2.0.0` |

On regular pushes to `main` (no tag) the pipeline only builds and tests — no packages are produced.

---

## Viewing results

| What | Where to find it |
|---|---|
| Pipeline runs | `https://github.com/sahinozd/LogicAppsStandard.Testing/actions` |
| Published NuGet packages | `https://github.com/sahinozd/LogicAppsStandard.Testing/packages` |
| GitHub Releases with `.nupkg` attachments | `https://github.com/sahinozd/LogicAppsStandard.Testing/releases` |
| Test results & coverage report | Download from the **Artifacts** section of each pipeline run |

---

## Comparison with Azure DevOps pipeline

| Azure DevOps concept | GitHub Actions equivalent |
|---|---|
| Variable group `Integration.TestFramework` | Repository secrets under **Settings → Secrets and variables → Actions** |
| `Build.BuildNumber` (patch version) | Git tag number (e.g. `42` in `v1.1.42`) |
| Azure Artifacts NuGet feed | GitHub Packages NuGet feed |
| `PublishBuildArtifacts` task | `actions/upload-artifact` |
| `##vso[task.setvariable]` | `echo "key=value" >> $GITHUB_OUTPUT` |
| Stages | Jobs (`needs:` for ordering) |
