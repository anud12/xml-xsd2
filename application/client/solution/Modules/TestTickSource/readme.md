# TestTick

A minimal module that creates an `mcv` entity on startup with `movementSpeed: 10`, `resource: 70`, and map coordinates `x: 0`, `y: 0` in its number map. Its dedicated window shows a placeholder portrait and a reactive resource bar ranging from 0 to 100. Movement speed remains stored data; clicking moves immediately rather than using that speed.

The screen contains a 10×10-cell map, displayed in an 800×600 logical-pixel group view. Its `emptyCell` texture uses the single 80×60-pixel `assets/map-cell.png` tile, with grid lines on its right and bottom edges; occupied cells instead show their member. Keep the tile, view dimensions, and group dimensions in sync. The MCV starts in the top-left cell and its portrait marks its position. Clicking a map cell teleports the MCV to that cell; the marker follows its coordinates. The `mcv-map` entity holds the `members` list and board dimensions, while the MCV's `stringMap` maps `xKey` and `yKey` to its `x` and `y` number-map properties. The view uses the same group for click-to-cell mapping. The map has a fixed logical size and may not cover viewports larger than 800×600.

Global values live on the entity with id `global`; its number-map key `Interest` starts at 0.

Run `npm run build` here to compile into the sibling `../TestTick` runtime directory. Copy `assets/` into `../TestTick/assets/` when deploying (TypeScript does not copy assets). The game archives `../TestTick` with paths relative to that directory.
