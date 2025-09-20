# Repository Guidelines

## Project Structure & Module Organization
- `Assets/Scenes/MainScene.unity`: Primary entry scene for local runs.
- `Assets/Scripts/`: C# code organized by domain (e.g., `MineralSystem`, `SampleSystem`, `SceneSystem`). Editor utilities live in `Assets/Scripts/Editor` and `Assets/Editor`.
- `Assets/Resources/{MineralData, Model, Localization}`: Runtime‑loaded content and data.
- `Packages/manifest.json`: Package sources (URP/HDRP, Input System, Test Framework; Git packages like GLTFUtility and unity‑mcp). Do not edit `packages-lock.json` manually.
- `ProjectSettings/`: Unity and render pipeline settings. Project targets Unity `6000.0.51f1`.

## Build, Test, and Development Commands
- Open: Unity `6000.0.51f1` → open the project folder.
- Run: Open `Assets/Scenes/MainScene.unity` and press Play.
- Tests (Editor): `Window → General → Test Runner` (EditMode/PlayMode).
- Tests (CLI example): `Unity -batchmode -projectPath . -runTests -testPlatform EditMode -logfile logs/test.log -resultsPath TestResults.xml -quit`.
- Build: Use `File → Build Settings…` to select target platform and scenes.

## Coding Style & Naming Conventions
- C#: 4‑space indent, Allman braces, one `MonoBehaviour` per file; filename matches the public class.
- Naming: PascalCase for types/methods/properties; camelCase for locals/params; prefer `_camelCase` private fields with `[SerializeField]` when exposed in Inspector.
- Namespaces: Match folder domains (e.g., `MineralSystem`, `SceneSystem`). Avoid `Find` calls; wire references via serialized fields.

## Testing Guidelines
- Framework: Unity Test Framework (`com.unity.test-framework`). Place tests in `Assets/Tests/EditMode` and `Assets/Tests/PlayMode`.
- Naming: `ClassNameTests.cs`; methods like `Method_Should_DoX_When_Y()`.
- Coverage: Add tests for new/changed logic; aim for meaningful coverage on core systems.

## Commit & Pull Request Guidelines
- Commits: Short, imperative summaries. Release commits follow `vX.Y.Z: Summary` (see history). Optionally prefix scope, e.g., `[MineralSystem] Fix …`.
- PRs: Clear description, linked issues, test notes, screenshots for UI, Unity version used, and affected scenes/systems. Include required `Packages/manifest.json` changes; exclude `Library/` and other generated files.

## Security & Configuration Tips
- Use Git LFS for large binaries; do not commit crash dumps or local caches.
- Do not hardcode secrets/paths; prefer config assets in `Resources/` when appropriate.
