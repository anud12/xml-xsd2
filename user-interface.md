# User Interface — Layout-first discussion

## Purpose

The objective of this specification is to define a minimal, high-impact subset of UI primitives and composition rules. By prioritizing "most bang for the buck," this API provides the essential building blocks required to assemble a fully functional and modern interface while maintaining a strictly limited architectural footprint.

The system focuses on a small, typed set of layout hints and primitive components that cover common UI needs without complex widget hierarchies.

---

## Design principles

- Layout-first: layout and positioning semantics are defined before visual styling.
- Declarative: the UI is expressed as a component tree; the runtime performs layout, rendering and event delivery.
- Logical coordinates: authors work in logical units; the runtime maps to device pixels with a scaleFactor.
- Minimal primitives: provide a small set of composable building blocks that cover common patterns.
- Explicit runtime contract: define the runtime obligations for layout, measurement, event mapping and accessibility.

---

## Panel

[see documentation](./user-interface/panel.md)

---

## Layout

[see documentation](./user-interface/layout.md)

## Division

[see documentation](./user-interface/division.md)

---

## Text

[see documentation](./user-interface/text.md)