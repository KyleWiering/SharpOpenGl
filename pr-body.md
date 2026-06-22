## Summary

Fixes the GitHub Pages 404 and the failed deploy workflow (#38).

## Root cause

1. **404** — Pages was serving the git `docs/` folder (markdown only, no `index.html`). The Blazor WASM build existed only in the Actions artifact, not in the committed `docs/` tree.
2. **Deploy failure** — The `gh api … build_type:workflow` step exited with code 1 and blocked the entire deploy (run [#38](https://github.com/KyleWiering/SharpOpenGl/actions/runs/27929107262)).

## Fix

- Build the site into `_site/` (keeps `docs/` as source-only guides in git)
- Deploy via `peaceiris/actions-gh-pages@v4` → `gh-pages` branch (reliable branch-based hosting)
- Best-effort API call to point Pages at `gh-pages` / `/` (`continue-on-error` so deploy never blocks)
- Keep `deploy-pages` artifact upload as a secondary path
- Add `.nojekyll`, `404.html`, and artifact verification

## CI actions (from prior commit)

- Upgraded `actions/checkout`, `setup-dotnet`, `upload-artifact` to v5 (Node 24 runtime)
- Removed obsolete `setup-node@20` terser step from `ci.yml`

## Verify after merge

1. [Deploy to GitHub Pages](https://github.com/KyleWiering/SharpOpenGl/actions/workflows/deploy-pages.yml) run succeeds
2. [Live demo](https://kylewiering.github.io/SharpOpenGl/) loads the Blazor app (not 404)
3. If still 404, set **Settings → Pages → Source** to `gh-pages` branch, `/` folder (one-time)