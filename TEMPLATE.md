# Unity Package Template - Setup Guide

> This file explains how to use the Unity package template. **Delete it once setup is complete**. It is also excluded from release archives via `.gitattributes`.

## Initialization steps

### 1. Replace all `@todo` markers

Search the repository for `@todo` and fill in or remove each one:

| File | Marker | What to put there |
|---|---|---|
| `package.json` | `"name"` | Reverse-domain package id, e.g. `com.side-xp.my-package` |
| `package.json` | `"displayName"` | Human-readable name shown in the Unity Package Manager |
| `package.json` | `"description"` | One-line package description |
| `README.md` | Title | Package display name |
| `README.md` | Description block | Short description + minimum Unity version (then remove the `<!-- @todo … -->` comment block) |
| `README.md` | Features section | Bullet list of features |
| `README.md` | Docs URL (`docs:remove` block) | Final GitHub Pages slug, e.g. `side-xp.github.io/unity-my-package` |
| `README.md` | Contributing (`docs:only` block) | Absolute CONTRIBUTING URL for this repo, e.g. `https://github.com/side-xp/unity-my-package/blob/main/CONTRIBUTING.md` |
| `Documentation~/zensical.toml` | `site_name` | Same display name as above |
| `Documentation~/zensical.toml` | `site_description` | Mirror of `package.json` `description` |
| `Documentation~/guides/getting-started.md` | Body | First-use instructions (rename/split into per-feature guides) |
| `CONTRIBUTING.md` | Minimum Unity version | Mirror of `package.json` `unity` |
| `CONTRIBUTING.md` | `instadoc --input …` | The source folders this package actually ships (local-preview only, CI auto-detects) |

> **Do not** create `Documentation~/README.md` (the docs index is generated from the root `README.md`) by CI (and is gitignored). For a local preview, run the `sed` command from `CONTRIBUTING.md` first.

### 2. Set the minimum Unity version

`package.json` defaults to `"unity": "6000.5"`. Change it if your package targets a different minimum version, and update the matching line in `CONTRIBUTING.md`.

### 3. Add your code (no asmdefs are shipped on purpose)

This template ships **no assembly definitions**, so it works equally well for runtime-only, editor-only, hybrid, demo-bearing, or asset-only packages. Add the structure your package needs, for example:

- `Runtime/<Asm>.asmdef`
- `Editor/<Asm>.Editor.asmdef`
- `Demos/Runtime/<Asm>.Demos.asmdef` + `Demos/Editor/<Asm>.Demos.Editor.asmdef`, gated behind a `SIDEXP_DEMOS` define constraint, if you ship samples.

The release workflow **auto-detects** `Runtime/`, `Editor/` and `Demos/` folders that contain C# files and feeds them to the API doc generator, so there is **nothing to edit in `release.yml`**. If the package has no C# at all, the API-generation step is simply skipped and the docs still build.

### 4. Configure the GitHub repository

- **Pages:** *Settings → Pages → Build and deployment → Source: **GitHub Actions*** (not "deploy from a branch"). The docs deploy to `https://side-xp.github.io/<repo-name>`.
- **Actions permissions:** *Settings → Actions → General → Workflow permissions* → **Read and write**, so the release job can push its release commit and tag, and create the GitHub Release.
- **Default branch** must be `main` (or `master`) so the release push trigger fires. Every push to it that contains a releasable commit (`feat`, `fix`, `perf` or a breaking change) publishes a release right away, so do day-to-day work on `develop` and merge into the default branch when you want to release.
- **Branch protection:** the release commit is pushed directly to the default branch with the default `GITHUB_TOKEN`. If a branch protection rule or ruleset requires pull requests on that branch, the push is rejected and no release is cut. To protect the branch anyway, the release job needs a token allowed to bypass the rules (e.g. a GitHub App token or an admin's PAT, stored as a secret), passed to both `actions/checkout` and the python-semantic-release step in `release.yml`.
- The `github-pages` environment is created automatically on the first docs deploy.
- Community health files (Code of Conduct, issue/PR templates, funding) are **inherited** from the org-level [`side-xp/.github`](https://github.com/side-xp/.github) (this repo intentionally ships none). `CONTRIBUTING.md` is the exception: it lives in this repo and is package-specific.

### 5. Delete this file

Once every `@todo` is resolved and the repo is ready for its first real commit, delete `TEMPLATE.md`.

## How the release workflow works

> **The workflow stays dormant until you configure the package.** A `guard` job runs first and skips everything while (a) the repo is still a GitHub template, or (b) `package.json` still contains any `@todo` placeholder. So a freshly generated repo won't cut a dummy `v1.0.0` or deploy `@todo` docs. The workflow activates automatically the moment you replace `@todo` in `package.json`.

Everything lives in a single workflow, [`.github/workflows/release.yml`](.github/workflows/release.yml), with three jobs (plus the guard above):

1. **release**: on each push to `main`/`master`, [python-semantic-release](https://python-semantic-release.readthedocs.io) reads your Conventional Commits since the last tag. If any is releasable, it bumps `package.json`'s `version`, updates `CHANGELOG.md`, pushes a release commit + tag and creates the GitHub Release. Its configuration lives in [`.github/release.toml`](.github/release.toml), which only sets what differs from the tool's defaults.
2. **build**: runs on a fresh release or via manual **workflow_dispatch**, from the release tag (not the pushed commit, so the docs and archives carry the bumped version and the updated changelog). It:
    - installs .NET + `SideXP.Instadoc` and generates the C# API reference into `Documentation~/api/` (auto-skipped when the package ships no C# sources),
    - generates the docs index from `README.md`, installs the Zensical `side-xp` theme and builds the site,
    - after a release only: `git archive`s the source into `<name>.zip` / `<name>.tar.gz` (honoring `.gitattributes`), zips `Documentation~/` (incl. the generated `api/`) into `Documentation_v<tag>.zip`, and attaches all of them to the Release.
3. **deploy**: deploys the site built by `build` to GitHub Pages.

Trigger a docs-only rebuild any time from the **Actions** tab via *Run workflow* (workflow_dispatch).

## Documentation structure

The `Documentation~/` folder (the tilde keeps Unity from importing it as package assets):

```
Documentation~/
  zensical.toml        # Zensical config (site name/description + shared side-xp theme)
  installation.md      # Installation instructions
  guides/              # One Markdown file per feature
    getting-started.md
  img/                 # Screenshots referenced by the guides (add as needed)
  (README.md  -> docs index, generated from the root README.md by CI; gitignored)
  (api/       -> C# API reference, generated by CI; gitignored)
  (site/      -> Zensical build output; gitignored)
  (.cache/    -> Zensical cache; gitignored)
```

Notes:

- The index page (`Documentation~/README.md`) is **generated** (never edit it by hand).
- The root `README.md` carries two kinds of HTML-comment markers used to derive the docs index: `docs:remove` (kept on GitHub, stripped from the docs site) and `docs:only` (hidden on GitHub, activated on the docs site). Keep links inside them in sync when you edit the README.
- A guide under `guides/` references images one level up (`../img/…`); the index page uses `./img/…`.
