/**
 * DivisionNode represents a positionless layout container for UI composition.
 *
 * - Divisions are recursively nestable; each forms an independent layout subtree.
 * - Parent arranges children using each child’s flex, alignment, and max properties.
 * - Cross-axis sizing: stretches to fill parent’s cross axis, capped by `max`.
 * - Division with no children still occupies its flex share.
 *
 * @see panel.md for PanelNode spec
 */
export type DivisionNode = {
  /**
   * The type of node. Always 'Division'.
   */
  type: 'Division';
  /**
   * Layout configuration for this Division. See type-level JSDoc for full details on each property and layout behavior.
   */
  layout: {
    /**
     * Main axis direction for child layout.
     * - 'Row': stack children horizontally (main axis = X)
     * - 'Column': stack children vertically (main axis = Y)
     *
     * Affects measurement, placement, and scroll direction.
     */
    direction: 'Row' | 'Column';
    /**
     * Main axis alignment of children.
     * - 'start': sequence begins at start edge; gap between items.
     * - 'center': sequence centered within available space.
     * - 'end': sequence ends at end edge.
     * - 'space-between': distribute remaining space as equal gaps between items (gap property is ignored). n==1 falls back to 'start'.
     */
    alignment: 'start' | 'center' | 'end' | 'space-between';
    /**
     * Gap (in logical units) between children along the main axis.
     * Used for spacing in all alignment modes except 'space-between', which computes its own inter-gap.
     */
    gap: NumberExpression;
    /**
     * Overflow handling for children that exceed available main axis space.
     * - 'clip': children outside visible bounds are clipped; no scrolling.
     * - 'wrap': children flow into additional lines using the wrap algorithm; each line is aligned independently.
     * - 'scroll': content is fully laid out on the main axis; a scrollOffset is applied; the container becomes scrollable.
     *
     * See detailed layout algorithm in type-level JSDoc.
     */
    clip: 'clip' | 'wrap' | 'scroll';
    /**
     * Flex share for this Division within its parent.
     * - 0 = fixed size
     * - >0 = proportional share of available space
     *
     * Used in parent layout to determine sizing.
     */
    flex: NumberExpression;
    /**
     * Maximum width for this Division (logical units).
     * Used to cap cross-axis sizing when direction is 'Column'.
     */
    maxWidth?: NumberExpression;
    /**
     * Maximum height for this Division (logical units).
     * Used to cap cross-axis sizing when direction is 'Row'.
     */
    maxHeight?: NumberExpression;
  };
  /**
   * Child Divisions (may be empty).
   *
   * - Each child is measured and placed according to the layout algorithm.
   * - Division with no children still occupies its flex share.
   * - Children may themselves be DivisionNodes or other leaf nodes.
   */
  divisions?: DivisionNode[];
};