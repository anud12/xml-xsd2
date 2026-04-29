# Stage_1 - Panel Position Assertions Suite

## Purpose

Stage 1 validates **panel position calculation correctness** in the rendering engine. This suite asserts that:

- Anchors properly center panels within parent container bounds
- Offsets correctly adjust panel positions from calculated anchor points  
- Sizes are applied without disrupting position calculations
- The coordinate system produces deterministic, predictable positioning results
