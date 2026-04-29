import {NumberExpression} from "../primitives/numberExpression";
import {TextureResource} from "../texture/TextureResource";
import {StringExpression} from "../primitives/stringExpression";

export type RegisterPanelFunction = (panelOptions: PanelOptions) => {}

export type PanelOptions = {
  id: string,
  /**
   * Components can anchor within their cell to control positioning and growth direction:
   *
   * - **0 (edge)**: Anchors to start/top; content grows rightward/downward
   * - **0.5 (center)**: Anchors to center; content grows symmetrically
   * - **1 (edge)**: Anchors to end/bottom; content grows leftward/upward
   */
  anchor?: { x: NumberExpression; y: NumberExpression };
  /**
   * Displacement in logical units from the aligned anchor/pivot point.
   * Used for fine-tuning panel position after anchor/pivot positioning.
   */
  offset?: {
    top: NumberExpression;      // Vertical offset from pivot Y (positive moves down)
    bottom: NumberExpression;   // Vertical offset from anchor Y (positive moves up)
    left: NumberExpression;     // Horizontal offset from pivot X (positive moves right)
    right: NumberExpression;    // Horizontal offset from anchor X (positive moves left)
  };
  size: { width: NumberExpression; height: NumberExpression };

  background: TextureResource;

  content?: PanelContent

  onClick?: PanelOnClickHandler;

  layout?: GridLayout,
  children?: PanelOptions[]
};

export type PanelContent = (EntityStringValueComponent
  | ConstantTextComponent
  ) & {
  align: "top"
    | "top-left"
    | "top-right"
    | "center"
    | "center-left"
    | "center-right"
    | "bottom"
    | "bottom-left"
    | "bottom-right"
}

export type EntityStringValueComponent = {
  type: "entityStringValue"
  name: StringExpression,
  entityId?: StringExpression,
}

export type ConstantTextComponent = {
  type: "constant",
  value: StringExpression
}

export type PanelOnClickHandler = {
  type: "emitAction",
  actionName: StringExpression
}

export type GridLayout = {
  columns?: TrackDefinition[];          // Column definitions; count = num columns
  rowFirst?: boolean;                  // true (default): left→right, wrap; false: top→bottom
  reverse?: boolean;                   // false (default): reverse child placement order
  gap?: { row?: NumberExpression; column?: NumberExpression };  // Cell spacing
};

export type TrackDefinition = SizeConstraint & {
  align?: "start" | "end";             // default: "start" — content alignment within track
};

export type SizeConstraint = {
  /**
   * Never sized below this
   */
  min?: NumberExpression;
  /**
   * Never sized above this (logical units)
   */
  max?: NumberExpression;
  /**
   * weight; claims remaining space after min
   */
  weight?: NumberExpression;
};
