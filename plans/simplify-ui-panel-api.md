# Plan: Simplify the UI Panel API

## Context

The `PanelOptions` surface (`application/suite/types/ui/Panel.d.ts`) is heavier than it needs to be for typical authoring. Concrete pain points observed in `MainModule/index.js` and the test modules:

1. **Placement**: both `anchor {x, y}` (fractions) and `offset {top, bottom, left, right}` (4 numbers) exist. A typical panel sets all 4 offsets to the same value and never uses anchor. 6 numbers to position one panel.
2. **Background**: `background` is typed as `AnimationRegistrationArguments` (frame arrays, sprite refs, durations). To show a single static PNG you must still go through the animation machinery (`getAnimation(name, {duration, loop})` with 1 frame).
3. **Content**: a 5-way discriminated union (`entityTextValue`, `entityNumberValue`, `constant`, `constantNumber`, `containerListView`) plus a repeated `align` discriminator. The value/constant pair and the text/number pair are the same shape.
4. **Layout**: `GridLayout` with `TrackDefinition` (`min`/`max`/`weight`/`align`) per column is full CSS-grid expressiveness; almost nothing in the codebase uses it.

Consumers of the API (all must be updated):
- `application/suite/types/ui/Panel.d.ts` — the TS contract
- `application/client/solution/Sources/Module/ContentParser.cs` — C# client (Jint) parses panel JSON
- `application/client/solution/Sources/Module/HostApiSetup.cs` — C# client host script (pre-evaluates `containerListView` templates)
- `application/client/solution/MainModule/index.js` + `Test/**/module/index.js` — consuming modules
- Rust runtime: `registerPanel` is a no-op there (`application/runtime/src/js_host_api/script_panel_entity.rs`), so no Rust parsing changes needed.

## Scope decision

**Simplify the shape, keep the semantics.** No new rendering features. The runtime keeps doing what it does; this plan only reduces authoring friction and API surface.

## Proposed new `PanelOptions` shape

```ts
type PanelOptions = {
  id: string
  size: { width: number; height: number }

  // placement: one of two modes, not both
  position?:
    | { mode: "center"; x?: number; y?: number }   // default (0,0) = screen center
    | { mode: "edge";   x: "left"|"right"; y: "top"|"bottom"; margin: number }

  // background: plain sprite string, animation only when actually animated
  background:
    | { sprite: string }                                            // static image (the common case)
    | { animation: string; duration: number; loop?: boolean }       // named animation
    | { frames: string[]; duration: number; loop?: boolean }        // inline frames

  content?:
    | { type: "text";  value?: string; entity?: { id: string; field: string }; align?: Align }
    | { type: "number"; value?: number; entity?: { id: string; field: string }; align?: Align }
    | { type: "containerList"; containerId: string; vertical?: boolean; template: (entity, index) => PanelOptions; align?: Align }

  hover?: { sprite: string; thickness: number }   // static hover sprite, no animation
  onClick?: { actionName: string }

  // layout: only what's used
  layout?: { columns?: number | number[]; gap?: number | { row: number; column: number } }
  children?: PanelOptions[]
}
```

### Change-by-change rationale

| # | Old | New | Why |
|---|-----|-----|-----|
| 1 | `anchor` + `offset {top,bottom,left,right}` | `position` union: `center {x?,y?}` or `edge {x,y,margin}` | Collapses 6 numbers to 1–3. `margin` replaces the 4-way offset (panels in the codebase always set all four equal). Edge naming is self-documenting. |
| 2 | `background: AnimationRegistrationArguments` (always) | 3-case union; `{ sprite: "x.png" }` for static | Static images stop requiring animation registration. `getSpritePNG` refs and inline frame arrays still supported. |
| 3 | `entityTextValue` / `constant` / `entityNumberValue` / `constantNumber` | `text` / `number`, each taking `value` **or** `entity {id, field}` | 4 types → 2. `value` present = constant; `entity` present = bound to entity. Same data, one discriminator. |
| 4 | `hover.texture: AnimationRegistrationArguments` | `hover.sprite: string` | Every hover in the codebase is a single static frame. Animations on hover can be re-added later if a real need appears. |
| 5 | `onClick: { type: "emitAction", actionName }` | `onClick: { actionName }` | Only one handler type exists; the discriminator is noise. |
| 6 | `GridLayout { columns: TrackDefinition[], rowFirst, reverse, gap }` | `layout: { columns?: number \| number[]; gap?: number \| {row, column} }` | `number` = N equal columns; `number[]` = per-column widths. Drops `min/max/weight/align` (unused) and `rowFirst`/`reverse` (unused; re-add if needed). |
| 7 | `align` required on content union wrapper | `align` optional per content variant, default `"center"` | Removes the wrapper indirection; parser already defaults to `"center"` (`ContentParser.cs:11`). |

**Not changed** (intentionally):
- `children` + recursive `PanelOptions` — nesting stays.
- `containerListView` → renamed `containerList` but same semantics: `template(entity, index) => PanelOptions`, `vertical` default true.
- Expression types (`NumberExpression`/`StringExpression`): the TS contract keeps them for now; the JS values produced by `number.of(...)` serialize to plain numbers/strings in the panel JSON anyway, so the *panel* types can be plain `number`/`string` without touching the rest of `HostApi`.

## Implementation steps

### 1. Update the TS contract
- Rewrite `application/suite/types/ui/Panel.d.ts` with the new `PanelOptions`.
- Delete `AlignOption` union? No — keep, it's the 9-position enum.
- `RegisterPanelFunction` unchanged.

### 2. Update C# client parser
- `ContentParser.cs`:
  - `ParsePanel`: read `position` instead of `anchor`+`offset`. Map to `Runtime.Vector2` anchor / `Runtime.Offset`:
    - `center {x,y}` → `Anchor (0.5, 0.5)`, `Offset` all = (x, y) as top/left deltas (verify against `Runtime.Panel` semantics in `Sources/Runtime`).
    - `edge` → `Anchor` at the named corner (0 or 1), `Offset` all = `margin`.
  - `ParsePanel` background: handle `{ sprite: string }`, `{ animation, duration, loop }` (resolve via registered animation by name — check how `ExtractTexture` currently digs out `name`/`frames[0].sprite`), and `{ frames: [...] }`.
  - `Parse` content: accept `text` / `number` with `value` or `entity`; map to existing `ConstantTextContent` / `EntityTextValueContent` / `ConstantNumberContent` / `EntityNumberValueContent`. **Keep the old `type` strings working for one release** (legacy fallback in the same parser) so existing test modules don't break until they're migrated.
  - `hover`: read `hover.sprite`.
  - `onClick`: read `onClick.actionName` (the `type` field is ignored).
  - `layout`: read `columns` as `number` or `number[]`; `gap` as `number` or object. (Check how `GridLayout` is consumed in `Sources/Runtime` — if the runtime ignores it for child panels today, the parser can store it but this is low risk.)
- `HostApiSetup.cs`: the `__templateResults` pre-evaluation block (line ~40) keys on `content.type === "containerListView"`. Update to also match `"containerList"`.

### 3. Migrate consuming modules
- `MainModule/index.js`:
  - `center` panel: `position: { mode: "edge", x: "left", y: "top", margin: 70 }`, `background: { animation: "texture", duration: 5, loop: true }`, `content: { type: "number", entity: { id: "entity_id", field: "key" }, align: "top" }`, `hover: { sprite: "hover.png", thickness: 5 }`.
  - `isModifiedPanel`: same pattern, `margin: 100/250`... wait — its offsets are top=100, left=250 (not all four equal). **Edge case:** the new `margin` single number can't express this. Options: (a) `margin: { top, left }` partial object, (b) keep `offset` as escape hatch. **Decision: `margin` may be `number | { top?, left?, right?, bottom? }`** — default fills all four. This keeps the common case at 1 number while remaining expressive.
- Test modules under `Test/Stage_*/...`, `Test/Ffi/Panel/...`: migrate each `index.js` to the new shape (small files, mechanical).
- Grep for any other `registerPanel` call sites after the first pass.

### 4. Rust runtime
- No changes: `registerPanel` is a no-op there. If the Rust side ever starts parsing panels, it will parse the new shape.

### 5. Legacy support policy
- Parser accepts old `type` discriminators (`entityTextValue`, `constant`, `entityNumberValue`, `constantNumber`, `containerListView`) and old `anchor`/`offset` for the duration of this sprint; remove in a follow-up once all modules are migrated. The TS contract does **not** document legacy forms.

## Verification

1. C# build: `dotnet build` in `application/client/solution` (verify the actual solution name on disk first).
2. Existing test modules: each `Test/**/module/index.js` is presumably exercised by a test harness — locate and run it (check `Test/` layout for a runner or xunit/nunit project).
3. Manual check of `MainModule`: launch the client, confirm `center` and `isModifiedPanel` render at the same positions as before, hover border thickness unchanged, background animation loops, and the number/text values still update as the `repeat` effect increments the entity.
4. Grep-verify no remaining references to removed discriminators in non-legacy code paths.

## Out of scope
- New layout modes (flex, auto-sizing).
- Animated hover.
- Changes to `HostApi.runtime` / entity / container APIs.
- Rust-side panel rendering.
