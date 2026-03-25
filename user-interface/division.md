## Division

A Division is a positionless layout container. It does not carry an (x,y) position — its size and position are determined entirely by the layout rules of its parent (a Panel or another Division). Divisions are the primary structural building block for composing interfaces.

Divisions may be nested recursively: a Division inside a Panel, a Division inside a Division, and so on. Each Division may declare its own direction, alignment, clip context, and flex sizing, forming independent layout subtrees.

---

### Properties

| Property         | Type                                              | Default    |
|------------------|---------------------------------------------------|------------|
| type             | "Division" \| "Row" \| "Column"                  | —          |
| layout.direction | "Row" \| "Column"                                 | "Column"   |
| layout.alignment | "start" \| "center" \| "end" \| "space-between"  | "start"    |
| layout.gap       | number (logical units)                            | 0          |
| layout.clip      | "clip" \| "wrap" \| "scroll"                      | "clip"     |
| layout.flex      | number (≥ 0)                                      | 1          |
| layout.max       | { width?: number, height?: number }               | none       |
| children         | ComponentNode[]                                   | []         |

Notes:
- "Row" and "Column" are convenience aliases for Division with `direction` preset.
- Cross-axis sizing: a Division stretches to fill the parent's cross axis by default, capped by `layout.max` on the cross axis.
- A Division with no children still occupies its flex share of space.

---

### Representation

```json
{
  "type": "Division",
  "layout": {
    "direction": "Column",
    "alignment": "center",
    "gap": 8,
    "clip": "scroll",
    "flex": 2,
    "max": { "height": 400 }
  },
  "children": [...]
}
```

Convenience aliases:

```json
{ "type": "Row",    "layout": { "alignment": "start",  "gap": 4 }, "children": [...] }
{ "type": "Column", "layout": { "alignment": "center", "gap": 8 }, "children": [...] }
```

---

### Flex sizing algorithm

Flex sizing controls how a Division's children share available main-axis space.

#### Step 1 — Classify children

- **Fixed child**: `layout.flex` absent or == 0. Main-axis size determined by content measurement.
- **Flex child**: `layout.flex` > 0. Main-axis size proportional to flex value within remaining space.

#### Step 2 — Measure fixed children

Measure all fixed children to obtain `fixedMain` per child.

```
fixedTotal = sum(fixedMain) + gap * (n - 1)   // n = total child count
```

#### Step 3 — Compute remaining space

```
remainingMain = availableMain - fixedTotal
```

If `remainingMain < 0` (fixed children already overflow), clamp to 0. The Division's `clip` property governs overflow visibility.

#### Step 4 — Distribute flex shares

```
totalFlex = sum(child.layout.flex for all flex children)

for each flex child:
  rawShare    = (child.layout.flex / totalFlex) * remainingMain
  childMain   = min(rawShare, child.layout.max[mainDimension] ?? Infinity)
```

Excess space freed by `max`-capped children is **not** redistributed to siblings (simple and predictable). Iterative redistribution may be added in a future revision if required.

#### Step 5 — Enforce cross-axis max

```
childCross = min(measuredCross, child.layout.max[crossDimension] ?? Infinity)
```

#### Step 6 — Apply Division's own max

After all children are sized and placed, clamp the Division's own computed size:

```
divisionWidth  = min(computedWidth,  layout.max.width  ?? Infinity)
divisionHeight = min(computedHeight, layout.max.height ?? Infinity)
```

---

### Layout algorithm (measure & place)

1. Classify children into fixed and flex groups.
2. Measure fixed children; compute `fixedTotal`.
3. Compute `remainingMain = availableMain - fixedTotal`.
4. Compute flex child sizes (Step 4 above).
5. Place all children in order according to `alignment`:
   - **start**: sequence begins at start edge; `gap` between items.
   - **center**: sequence centered within `availableMain`.
   - **end**: sequence ends at end edge.
   - **space-between**: remaining space distributed as equal gaps; `gap` ignored; n==1 falls back to "start".
6. Cross-axis: children align to cross-axis start edge using resolved cross size (capped by `max`).
7. Apply Division's own `max` to final computed size.

---

### Overflow, wrapping & scrolling

When children exceed `availableMain` after flex distribution, the Division's `clip` property governs behavior:

- **clip**: children outside visible bounds are clipped; no scrolling.
- **wrap**: children flow into additional lines (see Wrap algorithm); each line is aligned independently per `alignment`.
- **scroll**: content fully laid out on main axis; `scrollOffset` applied to placement; Division becomes scrollable.

#### Wrap algorithm (line formation)

Flex sizes are resolved first (without wrap knowledge); resolved sizes are then used for line formation to avoid circular dependencies.

1. `availableMain = mainSize`
2. `lines = []`, `currentLine = { items:[], usedMain:0, maxCross:0 }`
3. For each child in visual order:
   a. resolve child size → `childMain`, `childCross`
   b. If `currentLine` is empty: add child; `usedMain = childMain`; `maxCross = childCross`
   c. Else: `projected = currentLine.usedMain + gap + childMain`
      - If `projected <= availableMain`: add child; `usedMain += gap + childMain`; `maxCross = max(maxCross, childCross)`
      - Else: push `currentLine` to `lines`; start new `currentLine` with child
4. Push final `currentLine` to `lines`

#### Line placement & alignment

For each line (`n` = number of items):

```
lineOccupied = sum(childMain) + gap*(n-1)
extra        = availableMain - lineOccupied

interGap = (alignment == "space-between" && n > 1)
           ? (availableMain - sum(childMain)) / (n-1)
           : gap

startOffset:
  start:         0
  center:        extra / 2
  end:           extra
  space-between: 0

child position = paddingStart + startOffset + sum(prev childMain + interGap)
```

#### Cross-axis placement

- Each line height = `line.maxCross`.
- Lines stacked along cross axis with inter-line spacing = `gap`.
- Child cross position = cross-axis start of its line.

#### Content size

```
contentMainSize  = wrap ? max(lineOccupied per line) : sum(childMain) + gap*(n-1)
contentCrossSize = sum(lineHeight) + gap*(lineCount - 1)
```

#### Scroll behavior (`clip == "scroll"`)

```
maxScroll    = max(0, contentMainSize - mainSize)
scrollOffset ∈ [0, maxScroll]   (default 0)
renderPos    = computedPosition - scrollOffset   (main axis only)
```

Runtime API: `scrollTo(offset)`, `scrollBy(delta)`, `getScroll()`, optional `setScrollBehavior({ smooth: boolean })`.

Input mapping:
- Column Division: `wheel.y` → `scrollBy(deltaY)`
- Row Division: `wheel.x` → `scrollBy(deltaX)`; `shift+wheel.y` by convention

Keyboard: PageUp / PageDown / Arrow keys move by page or small increment; runtime must expose `scrollTo(nodeId)` for accessibility.

#### Oversize items

If `childMain > availableMain` and `currentLine` is empty: place child on its own line and allow overflow. Configurable runtime policy: `allowOverflow` (default), `clamp`, `scale-to-fit`.

---

### Determinism & rounding

All layout math occurs in logical units. After computing final positions (including scroll offsets), convert to device pixels via `scaleFactor` and apply deterministic snapping (round to nearest device pixel; tie-breaking rule documented by runtime).

---

### Edge cases

| Scenario | Behavior |
|---|---|
| `space-between` with n==1 | Falls back to "start" |
| All children have `flex == 0` | No flex distribution; treated as all-fixed |
| All children have `flex > 0`, no fixed children | Full `availableMain` distributed proportionally |
| Division `max` < its flex share | Takes only up to `max`; freed space is not redistributed to siblings |
| Repeated size changes (e.g. images loading) | Coalesce reflows per frame; use stable placeholder sizes |
| Empty children list | Division still occupies its flex share; renders nothing |
| Virtualization in scroll Division | Permitted; must not alter `contentMainSize` used for scroll extents |

---

### Accessibility & runtime surface

Expose per scrollable Division:
- `isScrollable`, `scrollOffset`, `maxScroll`, `contentSize`, `viewportSize`
- Event: `onScroll(startOffset, currentOffset, endOffset)`
- APIs: `scrollToNode(id)`, `scrollToOffset(offset)`, query scroll extents for screen readers

---

### Testing suggestions

- Flex share distribution: equal and unequal `flex` values across siblings.
- `max` cap on flex child: verify freed space is **not** redistributed.
- Wrap line formation with flex children: sizes resolved before wrapping.
- `space-between` with n==1 falls back to "start".
- Scroll offset clamping at `[0, maxScroll]`.
- Nested Division scroll contexts: inner and outer scroll do not interfere.
- Pixel snapping determinism at varying `scaleFactor` values.
