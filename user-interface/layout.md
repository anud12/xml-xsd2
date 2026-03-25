## Layout

Defines a simplified flexbox-like layout model with two container-level properties: Direction and Alignment. The goal is an easy-to-reason-about primitive that covers most common UI arrangements without the full complexity of CSS flexbox.

### Properties

- direction: "Row" | "Column"
  - Row: stack children horizontally (main axis = X).
  - Column: stack children vertically (main axis = Y).
- alignment: "start" | "center" | "end" | "space-between"
  - Distributes children along the main axis.
- gap?: number — logical units between items; defaults to 0.
- clip?: "clip" | "wrap" | "scroll" — overflow behaviour (defaults to "clip").

Notes:
- Cross-axis alignment is fixed to "start" in this simplified model.
- A container may be expressed as a convenience type ("Row"/"Column") or as a generic "Container" with layout.direction.

### Container representation

{
  "type": "Container",
  "layout": {
    "direction": "Column",
    "alignment": "center",
    "gap": 8,
    "clip": "scroll"
  },
  "children": [...]
}

### Layout algorithm (measure & place)

1. Measure all children → childMain, childCross per child.
2. totalChildrenSize = sum(childMain) + gap * (n - 1) + container paddings.
3. availableMain = containerMainSize - paddings.
4. Place children according to alignment:
   - start: sequence begins at start edge; gap between items.
   - center: sequence centered within availableMain.
   - end: sequence ends at end edge.
   - space-between: distribute remaining space as equal gaps between items (gap property is ignored). n==1 falls back to "start".
5. Cross-axis: children align to cross-axis start edge using each child's measured cross size.
6. If container size is "auto", the container shrinks or grows to fit its children.

### Overflow, wrapping & scrolling

When children exceed availableMain, the container's clip property determines behavior:

- clip: children outside visible bounds are clipped; no scrolling.
- wrap: children flow into additional lines using the wrap algorithm below; each line is aligned independently per the container's alignment setting.
- scroll: content is fully laid out on the main axis; a scrollOffset is applied to placement; the container becomes scrollable.

#### Terminology & variables

- direction ∈ {Row, Column}
- main axis = X for Row, Y for Column
- mainSize = container size on main axis minus paddings
- gap = layout.gap (default 0)
- childMain, childCross = measured sizes for each child on main/cross axes respectively

#### Wrap algorithm (line formation)

1. availableMain = mainSize.
2. lines = [], currentLine = {items:[], usedMain:0, maxCross:0}.
3. For each child in visual order:
   a. measure child → childMain, childCross.
   b. If currentLine is empty: add child; usedMain = childMain; maxCross = childCross.
   c. Else: projected = currentLine.usedMain + gap + childMain.
      - If projected <= availableMain: add child; usedMain += gap + childMain; maxCross = max(maxCross, childCross).
      - Else: push currentLine to lines; start a new currentLine with child.
4. Push final currentLine to lines.

#### Line placement & alignment

For each line (n = number of items in line):
- lineOccupied = sum(childMain) + gap*(n-1)
- extra = availableMain - lineOccupied
- interGap:
  - alignment == "space-between" && n > 1: interGap = (availableMain - sum(childMain)) / (n-1)
  - otherwise: interGap = gap
- startOffset (main axis):
  - start: 0
  - center: extra / 2
  - end: extra
  - space-between: 0
- Child positions: paddingStart + startOffset + sum(prev childMain + interGap)

#### Cross-axis placement

- Each line occupies lineHeight = line.maxCross on the cross axis.
- Lines are stacked along the cross axis in order with inter-line gap = gap.
- Child cross position defaults to cross-axis start of its line.

#### Content size

- contentMainSize = (wrap ? max(lineOccupied per line) : sum(childMain) + gap*(n-1))
- contentCrossSize = sum(lineHeight) + gap*(lineCount - 1)

#### Scroll behavior (clip == "scroll")

- maxScroll = max(0, contentMainSize - mainSize)
- scrollOffset ∈ [0, maxScroll]; default 0
- Render position = computedPosition - scrollOffset (applied on main axis only)
- Runtime API: scrollTo(offset), scrollBy(delta), getScroll(), optional setScrollBehavior({smooth: boolean})
- Input mapping:
  - Column container: wheel.y → scrollBy(deltaY)
  - Row container: wheel.x → scrollBy(deltaX); shift+wheel.y → scrollBy(deltaY) (runtime convention)
- Keyboard: PageUp/PageDown and Arrow keys move by page or small increments; runtime must expose programmatic scrollTo(nodeId) for accessibility.

#### Oversize items

If childMain > availableMain and currentLine is empty: place the child on its own line and allow it to overflow (contentMainSize will exceed mainSize). Runtime policies (configurable): allowOverflow (default), clamp to availableMain, scale-to-fit.

#### Clipping modes

- clip/hidden: render is constrained to the container viewport; no scroll offsets available.
- visible: children may render outside container bounds; overlays spawned by such children should use a separate panel/overlay context to avoid being clipped.

#### Determinism & rounding

All layout math occurs in logical units. After computing final positions (including scroll offsets), convert to device pixels by multiplying by scaleFactor and apply deterministic snapping (round to nearest device pixel; tie-breaking rule documented by runtime). Tests rely on deterministic measureText and deterministic reflow ordering.

#### Edge cases

- Repeated size changes (e.g., images loading): coalesce reflows per frame; provide stable placeholder sizes where possible.
- space-between with n==1: treat as "start".
- Virtualization: permitted for large scrollable lists; virtualization must not change contentMainSize used to compute scroll extents.

#### Accessibility & runtime surface

- Expose per-container: isScrollable, scrollOffset, maxScroll, contentSize, viewportSize.
- Events: onScroll(startOffset, currentOffset, endOffset).
- Accessible APIs: scrollToNode(id), scrollToOffset(offset), query scroll extents for screen readers.

#### Testing suggestions

Unit tests for: wrap line formation and item distribution, interGap math for space-between, oversized single-child handling, scroll offset clamping at min/max, pixel snapping determinism at varying scaleFactor values.
