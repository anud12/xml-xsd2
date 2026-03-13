# Copilot instructions — xml-xsd2

Purpose

Short guide for Copilot CLI sessions running against this repository. Focuses on the repo's OpenSpec workspace, available prompts/skills, and concrete commands used by the prompts.

---

Build / test / lint

- No package manifests or conventional build/test/lint entries were detected in the repo root.
- Primary CLI surfaced in prompts/skills: the repository's prompts assume the `openspec` CLI is available. Key commands referenced by prompts/skills:
  - openspec new change "<name>" — scaffold a change at `openspec/changes/<name>/`
  - openspec status --change "<name>" --json — return artifacts list and `applyRequires`
  - openspec instructions <artifact-id> --change "<name>" --json — return `template`, `instruction`, `context`, `rules`, `outputPath`, and `dependencies`
  - openspec status --change "<name>" — show human-friendly status

(If project code or tests are added later, add the corresponding manifest and update this file with exact build/test/lint commands and how to run a single test.)

---

High-level architecture

- This repository is organized as an OpenSpec workspace:
  - openspec/config.yaml — project-level spec-driven configuration (project context and per-artifact rules)
  - openspec/specs/ — canonical spec artifacts (domain/spec files)
  - openspec/changes/ — per-change workspaces containing artifacts and a `.openspec.yaml` scaffold
- Copilot automation artifacts live under `.github/`:
  - .github/prompts/ — opsx-*.prompt.md files (starter prompts used by Copilot CLI flows)
  - .github/skills/ — openspec-* skill folders; each contains a SKILL.md describing capability and constraints
- The typical interaction flow (as encoded in prompts/skills):
  1. Create or open a change: `openspec new change "<name>"`
  2. Query `openspec status --change "<name>" --json` to discover `artifacts` and `applyRequires`
  3. For each ready artifact, fetch `openspec instructions <artifact-id> --change "<name>" --json` and generate the artifact file using the provided `template` and `instruction`
  4. Repeat until all `applyRequires` artifacts are complete, then run status to confirm readiness for implementation

---

Key conventions (repo-specific)

- Prompts and skills pairing
  - Prompt files are named `opsx-<action>.prompt.md` under `.github/prompts/`.
  - Skills are in `.github/skills/` and typically use the `openspec-<action>` naming convention. Each skill includes a SKILL.md with metadata (compatibility, license, author).
  - The prompts and skills map 1:1 by action name (e.g., `opsx-propose` ↔ `openspec-propose`).

- Artifact generation rules (important for Copilot output)
  - Use `openspec instructions` to obtain: `template`, `instruction`, `context`, `rules`, `outputPath`, and `dependencies`.
  - Follow the `template` and `instruction` for structure and content.
  - `context` and `rules` are constraints and MUST NOT be copied into generated artifact files. They guide content but are not part of the artifact.
  - Always read dependency artifacts (listed in `dependencies`) before writing a new artifact.
  - Use `applyRequires` from `openspec status --json` to know which artifacts must be completed before implementation.

- Tools referenced by prompts
  - AskUserQuestion tool — used to request missing clarifications from the user (open-ended questions).
  - TodoWrite tool — used by some workflows to track progress while generating multiple artifacts.

- Guardrails encoded in prompts/skills (follow strictly)
  - Create all artifacts required for implementation as defined by the schema's `apply.requires`.
  - Verify artifact files exist after writing before proceeding.
  - If critical context is missing, ask the user; otherwise make reasonable, minimal assumptions to keep progress.

---

Where to look first (authoritative files)

- .github/prompts/ (starter prompts for common workflows)
- .github/skills/*/SKILL.md (skill metadata & compatibility; many skills require the `openspec` CLI)
- openspec/config.yaml (project context, per-artifact rules)
- openspec/changes/ (existing change drafts)

---

Common prompt ↔ skill mappings

- opsx-propose.prompt.md ↔ openspec-propose
- opsx-new.prompt.md ↔ openspec-new-change
- opsx-apply.prompt.md ↔ openspec-apply-change
- opsx-verify.prompt.md ↔ openspec-verify-change
- opsx-sync.prompt.md ↔ openspec-sync-specs
- opsx-archive.prompt.md ↔ openspec-archive-change
- opsx-bulk-archive.prompt.md ↔ openspec-bulk-archive-change
- opsx-continue.prompt.md ↔ openspec-continue-change
- opsx-ff.prompt.md ↔ openspec-ff-change
- opsx-onboard.prompt.md ↔ openspec-onboard
- opsx-explore.prompt.md ↔ openspec-explore

---

Quick Copilot session checklist

- Load `openspec/config.yaml` into context when starting work on a change (it contains the project context and rules).
- Start with the matching opsx-*.prompt.md for the desired workflow.
- Ensure `openspec` CLI is available in the environment when running skills that require it.
- Honor `instruction` and `template` from `openspec instructions` responses and do NOT output `context`/`rules` blocks inside artifacts.
- Use AskUserQuestion for missing user input and TodoWrite to track multi-artifact work.

---

Notes for maintainers

- Add concrete build/test/lint instructions here if/when code is added (package.json, pyproject.toml, etc.).
- Keep .github/prompts and .github/skills in sync: changes to a prompt's expected inputs should be reflected in the corresponding SKILL.md metadata.

---

If anything in this file should cover additional areas (e.g., a language-specific test runner or CI job), tell me where to look and it will be added.
