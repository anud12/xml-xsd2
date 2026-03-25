# Text Component

A leaf UI component for rendering a single line of plain text within a Division.

## Properties
- **value**: Required. A UTF-8 string. All characters, including emojis and control characters, are accepted. Null or empty renders an empty box.
- **maxWidthPx**: Required. Maximum width in pixels. If set to zero or negative, clamps to 0px and renders nothing.
- **ellipsis**: Optional. If true and text overflows, display “…” at the end. If false or omitted, text is simply truncated.

## Behavior
- Renders a single line of text using a fixed font and style defined by the runtime.
- All whitespace (leading, trailing, multiple spaces) is preserved.
- No formatting, markup, or variable interpolation.
- No accessibility (screen reader or tooltip) support for truncated text.

## Edge Cases
- Overflow: Truncate to maxWidthPx; add ellipsis if enabled.
- Only whitespace: Renders as a wide empty space.
- Control characters: Render as-is (may be invisible or special glyphs).
- maxWidthPx ≤ 0: Renders nothing.
- Font/style: Fixed by runtime, ensuring consistent measurement.

## Example
| value           | maxWidthPx | ellipsis | Rendered Result           |
|-----------------|------------|----------|--------------------------|
| "Hello world"   | 100        | false    | Hello world              |
| "Hello world"   | 40         | true     | Hel…                     |
| "     "         | 50         | false    | (wide empty space)       |
| null            | 50         | false    | (empty box)              |
| "🐤🐤🐤🐤🐤"      | 30         | true     | 🐤…                      |
