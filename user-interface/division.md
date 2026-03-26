## Division (Essence)

A Division is a positionless layout container. Its size and position are determined by the layout rules of its parent (Panel or Division). Divisions are the primary structural building block for composing interfaces and may be nested recursively.

### HostApi
```typescript
export type DivisionNode = {
  type: 'Division';
  layout: {
    direction: 'Row' | 'Column';
    alignment: 'start' | 'center' | 'end' | 'space-between';
    gap: NumberExpression;
    clip: 'clip' | 'wrap' | 'scroll';
    flex: NumberExpression;
    max: { width?: NumberExpression; height?: NumberExpression };
  };
  divisions?: DivisionNode[];
};
```

### Layout & Sizing
- Divisions may be nested; each forms an independent layout subtree.
- Cross-axis sizing: stretches to fill parent’s cross axis, capped by `layout.max`.
- Division with no divisions still occupies its flex share.

### Flex Sizing Algorithm
- Children classified as fixed (`layout.flex` absent/0) or flex (`layout.flex` > 0).
- Fixed children sized by content; flex children share remaining space proportionally.
- `max` caps are enforced per child and for the Division itself.
- No redistribution of freed space from capped children.

### Placement & Alignment
- Children placed in order according to `alignment`:
  - start: from start edge
  - center: centered
  - end: from end edge
  - space-between: equal gaps (n==1 falls back to start)
- Cross-axis: children align to start edge, capped by `max`.

### Overflow, Wrapping & Scrolling
- `clip`: children outside bounds are clipped
- `wrap`: children flow into lines; each line aligned independently
- `scroll`: content fully laid out; Division becomes scrollable

### Rounding
- All layout math in logical units; convert to device pixels via scaleFactor and apply deterministic snapping
