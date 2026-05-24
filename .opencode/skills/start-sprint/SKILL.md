---
name: start-sprint
description: Trigger full agentic delivery pipeline autonomously — no questions asked
license: MIT
compatibility: opencode
metadata:
  audience: sprint-leads, automation-crafters
  workflow: agentic-delivery-pipeline
  autonomy-mode: true
---

## What I do

- **Autonomously trigger** the full agentic delivery pipeline (Note Breaker → Implementer → Validator → PR Publisher)
- **Provide upfront context** to all agents so they don't ask questions when flow starts
- **Handle errors gracefully** without pausing for clarification — attempt fixes or skip tasks if blocked
- **Generate a sprint status report** at the end showing completed tasks and any blockers

## When to use me

Use this when you want to start an autonomous agentic sprint. The pipeline will:
1. Read all `.agentic/notes/*.txt` files (excluding `README.txt`)
2. Convert notes into half-day task files in `.agentic/tasks/` via Note Breaker
3. Have Implementer pick up each task sequentially, create branches, implement work
4. Run Validator to run `cargo test` on each task's changes
5. Push validated branches to GitHub via PR Publisher

**Autonomous mode means:** agents will NOT ask questions when the flow starts. They'll use context provided in this skill and proceed with best-effort implementation. Only true blockers (authentication failures, missing dependencies) will halt work — and even then, they'll attempt alternatives before stopping.

## Autonomous Workflow Instructions

### PHASE 0: INITIALIZATION
Read all `.agentic/notes/*.txt` files. Extract every actionable item and proceed to Phase 1 without asking questions.

---

### PHASE 1: TASK GENERATION (Note Breaker)
Process each note file in alphabetical order:

1. Read the full note content
2. Identify discrete work items (each should be ~3-4 hours of work)
3. If a note describes a large feature, decompose it into sequential sub-tasks linked by dependencies
4. Create task files in `.agentic/tasks/` with naming: `TASK-YYYY-MM-DD-NNN-short-name.txt`
5. Assign priorities: high (blocks others), medium (standalone), low (polish)
6. Generate summary in `.agentic/tasks_summary.txt`

**Autonomous rules:**
- Do NOT ask questions about unclear notes — create a task with `PRIORITY: high` and `STATUS: blocked_until_clarification`
- Never modify existing tasks — only create new ones
- Proceed to next note without waiting for user confirmation

---

### PHASE 2: IMPLEMENTATION (Implementer)
Pick up each task from the task summary in sequential order:

1. Read task file from `.agentic/tasks/TASK-XXX.txt`
2. Create branch: `task/TASK-YYYY-MM-DD-NNN-short-name`
3. Make todo list with `[agent-name]` prefix for each sub-step
4. Delegate to sub-agents where appropriate (code-gen, refactor, docs, debugger)
5. Commit with Conventional Commits: `feat(TASK-XXX): description`, `fix: ...`, etc.
6. Update task status to `COMPLETE` when done

**Autonomous rules:**
- Do NOT ask questions about requirements — infer from the note source and task file
- If a sub-agent fails, attempt alternative approach or skip to next sub-task rather than asking
- Never pause for clarification during implementation
- Mark task as blocked only if truly impossible (e.g., missing files referenced in description)

---

### PHASE 3: VALIDATION (Validator)
After Implementer completes and hands off:

1. Run `cargo test` from `E:\workspace\xml-xsd2\application\runtime`
2. Parse output for total tests, passed, failed, compilation errors
3. If pass: report success and signal PR Publisher to proceed
4. If fail: list ALL failures with full error messages, then ping Implementer with detailed analysis

**Autonomous rules:**
- Do NOT ask questions about how to fix — provide the complete failure report and let Implementer decide
- If a test consistently fails across 2+ attempts after fixing, mark task as blocker in task file
- Proceed to next task without waiting for human input on fixes

---

### PHASE 4: PUBLICATION (PR Publisher)
When all tests pass and Implementer signals completion:

1. Verify branch exists locally
2. Push branch to origin
3. Check for existing PR — if not, create draft PR with task metadata from task file
4. Generate PR body with task info, acceptance criteria, and files changed
5. Report success or provide alternative resolution paths for failures

**Autonomous rules:**
- If push conflicts: suggest `git pull --rebase origin <branch-name>` command in report
- If auth fails: instruct user to run `gh auth login` or set `GITHUB_TOKEN`, then retry same branch
- If task file missing: proceed with branch name as fallback title, note "task file not found" in PR body

---

### PHASE 5: FINAL REPORTING
After all tasks complete (or are blocked):

Generate final sprint report:
- Total tasks processed
- Completed tasks with PR links
- Blocked tasks with reasons
- Time spent per phase

**Autonomous rules:**
- Do NOT ask questions about which tasks to prioritize next — leave that for user
- Report blockers clearly but don't halt the entire pipeline on one blocked task (unless truly blocking)

---

## Strict Restrictions — Autonomous Mode

**NEVER ask clarifying questions** when the flow has started. This includes:
- Ambiguities in task requirements — infer from context and source note
- Unclear acceptance criteria — make best-effort implementation based on title + description
- Implementation choices — use reasonable defaults (e.g., prefer existing patterns, don't reinvent)

Only ask questions when the workflow is genuinely blocked by external factors:
- Authentication failures that can't be resolved automatically
- Missing repository access or credentials
- Truly ambiguous requirements that make any implementation guess risky

---

## Proficiency

- **Autonomous orchestration** — coordinating Note Breaker → Implementer → Validator → PR Publisher without human intervention
- **Task generation** — converting plain-text notes into structured task files
- **Branch creation and commit discipline** — following Conventional Commits, atomic commits for file moves
- **Error diagnosis** — identifying root causes of test failures or compile errors
- **Ping-pong coordination** — validator → implementer → validator fix cycles without asking questions
- **Git workflow automation** — pushing validated branches, creating draft PRs with metadata

---

## Workspace Context

### Project Layout
- **Rust runtime**: `E:\workspace\xml-xsd2\application\runtime\` — core game engine logic
- **C# Godot client**: `E:\workspace\xml-xsd2\application\client\solution\` — UI and game presentation layer
- **Java test suite**: `E:\workspace\xml-xsd2\application\suite\` — integration and acceptance tests
- **Task system**: `E:\workspace\xml-xsd2\.agentic\tasks\` — task files (TASK-YYYY-MM-DD-NNN-short-name.txt)
- **Agent notes**: `E:\workspace\xml-xsd2\.agentic\notes\` — source notes for task breakdown

### Task File Format
Each task file in `.agentic/tasks/` contains:
- **Task ID and title** — identifying the work item
- **Description** — what to implement
- **Acceptance criteria** — how we know the task is done
- **Dependencies** — any prerequisite tasks (if applicable)
- **Status** — one of: `PENDING`, `IN_PROGRESS`, `COMPLETE`, `BLOCKED`

### Branch Naming Conventions
- Task implementation branches: `task/TASK-XXX`
- Existing convention in repo also includes: `feature/...`, `fix/...`, `refactor/...`
- Use the `task/` prefix for implementer-created branches to distinguish them from ad-hoc work

---

## Strict Restrictions — Do Not Violate

You are NOT allowed to edit any of the following folders under any circumstances:

- The "Test" folder under the "client" C# project: `E:\workspace\xml-xsd2\application\client\solution\Test\`
- The features folder under "suite" Java project: `E:\workspace\xml-xsd2\application\suite\src\test\resources\features\`
- The tests folder under "suite" Java project: `E:\workspace\xml-xsd2\application\suite\src\test\java\com\example\tests\`

These directories contain test fixtures, test steps, and feature definitions that are managed exclusively by the validation/test pipeline. Any modification to test data, feature files, or test step code must be handled by a dedicated test agent or approved by the task owner.