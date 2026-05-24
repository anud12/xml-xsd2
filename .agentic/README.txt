AGENTIC WORKFLOW
================
This directory contains all agentic workflow information for the xml-xsd2
project. Everything related to the agent-driven development process lives
here.

FOLDER STRUCTURE
----------------
.agentic/
  notes/       — Drop your plain-text (.txt) notes here
  tasks/       — Generated tasks (auto-populated by agents)
  README.txt   — This file

.agents/       — Agent diagnosis and investigation reports (separate from workflow)

AVAILABLE AGENTS (in .opencode/agents/)
---------------------------------------
1. note-breaker  — Reads notes from .agentic/notes/, breaks them into
                    half-day actionable tasks, writes them to .agentic/tasks/

2. implementer   — Picks up a task, creates a git branch, plans via todo
                    with agent prefixes, delegates to sub-agents, handles
                    errors recursively, commits all changes, signals for
                    validation

3. validator     — Runs cargo test in application/runtime, reports pass/fail,
                    pings back to implementer on failures (ping-pong cycle
                    until all tests pass)

HOW TO USE THE WORKFLOW
------------------------
STEP 1: WRITE YOUR NOTES
  Create one or more .txt files in .agentic/notes/ describing what you want
  built. Use plain language. Include as much detail as possible.

  Example note (features.txt):
    Title: Add entity number map export
    Description: The runtime needs to expose a function that returns a map
      of entity IDs to their display numbers. This should be callable from
      the C# client via FFI.
    Context: The entity system is in runtime/src/entities/
    Acceptance: cargo test passes, the function is exported, the C# client
      can call it

STEP 2: INVOKЕ NOTE-BREAKER
  Ask the note-breaker agent to process your notes. It will:
  - Read all .txt files in .agentic/notes/
  - Decompose each note into half-day tasks
  - Write task files to .agentic/tasks/
  - Generate a _summary.txt with all tasks listed

STEP 3: INVOKЕ IMPLEMENTER
  Give the implementer agent a task file (or let it pick the next pending
  task). It will:
  - Create a git branch (task/TASK-XXX)
  - Create a todo list with agent prefixes like [agent-name]
  - Delegate sub-tasks to sub-agents
  - Handle errors by spawning recursive sub-agents
  - Commit all changes with proper file-move discipline
  - Signal when ready for validation

STEP 4: INVOKЕ VALIDATOR
  After implementation, the validator agent runs cargo test in the runtime
  directory. If tests pass, the task is verified. If tests fail, it pings
  back to the implementer with detailed failure info, and the cycle repeats.

THE PING-PONG CYCLE
--------------------
  Implementer finishes → Validator runs tests → Tests fail? → Validator
  reports failures → Implementer fixes → Validator re-runs → Tests pass?
  → Task verified ✓

  This cycle repeats until all tests pass. Neither agent edits the
  protected test folders.

STRICT RESTRICTIONS (ALL AGENTS)
---------------------------------
NO agent is allowed to edit:
  - E:\workspace\xml-xsd2\application\client\solution\Test\
  - E:\workspace\xml-xsd2\application\suite\src\test\resources\features\
  - E:\workspace\xml-xsd2\application\suite\src\test\java\com\example\tests\

GIT DISCIPLINE
--------------
  - Every task gets its own branch (task/TASK-XXX)
  - File moves follow: create dir → commit, move file → commit, update →
    commit, delete old → commit
  - All commits reference the task number
  - Conventional commits format: feat(TASK-XXX): description

TOOL CALLING FORMAT
--------------------
  When calling a tool, always add a new line before calling it to better
  define the start point of the tool call.
