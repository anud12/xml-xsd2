import { DivisionNode } from "./division";

/**
 * PanelNode defines a logical coordinate and clipping context for child placement, anchoring, and overlays.
 * All child positions are in the panel’s logical space. Multiple panels are supported; events and measurements are reported per panel.
 *
 * Coordinate System:
 * - Origin (0,0) is top-left; X→right, Y→down
 * - Units are logical pixels; runtime maps to device pixels via `scaleFactor`
 * - Deterministic text measurement and rounding required
 *
 * Anchors:
 * - Only edges/corners: "top-left", "top", "top-right", "left", "right", "bottom-left", "bottom", "bottom-right"
 * - No center anchor
 * - Each anchor maps to (ax, ay) in [0,1] for placement and transforms
 * - Defaults: anchor = "top-left", transformOrigin = anchor
 * - See anchor table in division.md for mapping details
 *
 * Placement:
 * - Absolute: Node’s anchor point is placed at (x, y) in panel space, with optional offset
 * - Relative/flex layouts ignore anchor for placement
 *
 * Overlays:
 * - Positioned using targetAnchor (on target) and overlayAnchor (on overlay), plus offset
 * - Placement policy: try requested alignment, flip if overflow, clamp if still overflowing
 *
 * Rounding:
 * - Logical → device pixels via scaleFactor, with deterministic snapping
 *
 * Intended Usage:
 * - Use PanelNode to define a root or overlay context for UI composition.
 * - All layout and measurement logic is explicit and deterministic.
 *
 * @see DivisionNode
 * @see division.md
 */
export type PanelNode = {
  /**
   * Unique identifier for the panel instance.
   */
  id: string;
  /**
   * Optional absolute position of the panel’s anchor point in parent space.
   * @see anchorDefault
   */
  position?: { x: NumberExpression; y: NumberExpression };
  /**
   * Size of the panel in logical pixels. Required.
   * Both width and height must be specified.
   */
  size: { width: NumberExpression; height: NumberExpression };
  /**
   * Clipping policy for content that overflows the panel bounds. Required.
   * - 'visible': No clipping
   * - 'hidden': Clip to panel bounds
   * - 'scroll': Enable scrolling for overflow
   */
  clip: 'visible' | 'hidden' | 'scroll';
  /**
   * Logical-to-device pixel scale factor. Used for deterministic rounding and measurement.
   */
  scaleFactor?: NumberExpression;
  /**
   * Anchor for child placement and transforms. Required.
   * Only edges/corners allowed.
   *
   * | Anchor        | ax   | ay   |
   * |-------------- |------|------|
   * | top-left      | 0    | 0    |
   * | top           | 0.5  | 0    |
   * | top-right     | 1    | 0    |
   * | left          | 0    | 0.5  |
   * | right         | 1    | 0.5  |
   * | bottom-left   | 0    | 1    |
   * | bottom        | 0.5  | 1    |
   * | bottom-right  | 1    | 1    |
   */
  anchor:
    | 'top-left' | 'top' | 'top-right'
    | 'left' | 'right'
    | 'bottom-left' | 'bottom' | 'bottom-right';
  /**
   * List of DivisionNode children to be rendered within this panel.
   */
  divisions: DivisionNode[];
};
