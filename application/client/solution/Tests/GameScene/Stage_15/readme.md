# Stage 15 - UI Traceability

## Purpose

Stage 15 validates **UI interaction traceability and replay**.

A user's clicks are recorded as "what" (the node id), "when" (the UI frame,
relative to the first event — first event = frame 0), and "where" (the cursor
position). Each interaction is appended to a `.replay` file (JSON lines) by
the background **Logging** thread, off the main thread. The replay file is
self-contained: reading it back and re-driving the recorded interactions (pumping
the UI to each recorded frame) reproduces the user's actions against the
runtime, which is used to validate the test.

## Layout

Two folders exercise the same traceability/replay pipeline over different node
kinds. Each has a `module/` (the UI + an `onClick` that emits a named action and
logs a sentinel line when it fires), a committed `<node>.expected.replay.jsonl`
at the folder root (the **expected** replay — the replay test's input and the
reference the emitted output is asserted against), and a test class with three
tests:
**Replay** (re-drive the expected file, pumping the UI to each recorded frame,
and assert the recorded actions fire, no live clicking), **Emitted file** (click
once per expected event, then assert the emitted `<node>.actual.replay.jsonl`'s
`(type, node, pointer)` per event + count match the expected — the pointer is the
recorded click point — with ordered `uiFrame`, and validate against the schema),
and **Schema** (reject malformed replay lines).

- `window_OnClick/` — the `onClick` lives on the window itself (node `buy-btn`).
  `WindowOnClick.cs` targets the window.
- `Panel_OnClick/` — the `onClick` lives on a nested panel window (`click-panel`)
  inside a host window. Clicking the nested panel records the panel's own node id,
  not the host's. `PanelOnClick.cs` targets the nested panel.

## Recording / replay file

One JSON object per line in `<test dir>/<node>.expected.replay.jsonl`:

```json
{"type":"ui.click","node":"buy-btn","uiFrame":0,"pointer":{"x":100,"y":200}}
```

Each line keeps `type`, `node` (the "what"), `uiFrame` (the "when" — the UI frame
relative to the first event in the session) and `pointer` (the cursor position
when the click fired, the "where"). Replay pumps the UI to each recorded frame
and re-runs the node's `onClick` via `UiJsClick`.

## Schema validation

Every line must validate against
`Sources/UI/UiTraceability/replay.schema.json` (JSON Schema, embedded in the
assembly and loaded once). `UiTraceability.ValidateEvent` / `ValidateFile`
enforce it (via NJsonSchema), and `ReadFile` validates each line as it reads,
so a corrupt or hand-edited replay file fails loudly instead of mis-driving the
replay.
