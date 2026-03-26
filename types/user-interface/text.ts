/**
 * TextNode is a leaf UI component for rendering a single line of plain text within a Division.
 *
 * Purpose & Constraints:
 * - Renders a single line of UTF-8 text using a fixed font and style defined by the runtime.
 * - All whitespace (leading, trailing, multiple spaces) is preserved.
 * - No formatting, markup, or variable interpolation.
 * - No accessibility (screen reader or tooltip) support for truncated text.
 * - Font and style are fixed by the runtime for deterministic measurement.
 *
 * Intended Usage:
 * - Use TextNode as a child of DivisionNode to display unformatted, single-line text.
 * - For multi-line or formatted text, use a different component.
 *
 * @see DivisionNode
 * @see text.md
 */
export type TextNode = {
  /**
   * The type of node. Always 'Text'.
   */
  type: 'Text';
  /**
   * The text value to render. Required. UTF-8 string. Null or empty renders an empty box.
   */
  value: StringExpression;
  /**
   * Maximum width in pixels. Required. If zero or negative, clamps to 0px and renders nothing.
   */
  maxWidthPx: NumberExpression;
  /**
   * If true and text overflows, display “…” at the end. If false or omitted, text is simply truncated.
   */
  ellipsis?: ConditionExpression;
};
