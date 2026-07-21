import {NumberExpression} from "../primitives/numberExpression";
import {TextureResource} from "../texture/TextureResource";
import {StringExpression} from "../primitives/stringExpression";
import {Entity, EntityExpression} from "../Entity";

/**
 * Callback signature for registering a panel with the UI host.
 * Accepts {@link PanelOptions} and returns an empty result object.
 */
export type RegisterPanelFunction = (panelOptions: PanelOptions) => {}

/**
 * Configuration options for a panel registered via `hostApi.ui.registerPanel`.
 */
export type PanelOptions = {
  /** Unique identifier for this panel. */
  id: string,
  /**
   * Anchor point within the panel's cell, expressed as fractions (0–1).
   *
   * - **0**: Anchors to the start/top edge; content grows rightward/downward.
   * - **0.5**: Anchors to center; content grows symmetrically.
   * - **1**: Anchors to the end/bottom edge; content grows leftward/upward.
   */
  anchor?: { x: NumberExpression; y: NumberExpression };
  /**
   * Displacement in logical units from the anchored position.
   * Used for fine-tuning panel placement after anchor positioning.
   */
  offset?: {
    /** Vertical offset from pivot Y; positive values move down. */
    top: NumberExpression;
    /** Vertical offset from anchor Y; positive values move up. */
    bottom: NumberExpression;
    /** Horizontal offset from pivot X; positive values move right. */
    left: NumberExpression;
    /** Horizontal offset from anchor X; positive values move left. */
    right: NumberExpression;
  };
  /** Panel dimensions in logical units. */
  size: { width: NumberExpression; height: NumberExpression };
  /** Background texture applied to the panel. */
  background: TextureResource;
  /** Content component rendered inside the panel. */
  content?: PanelContent
  /** Handler invoked when the panel is clicked. */
  onClick?: PanelOnClickHandler;
  /** Hover state configuration. */
  hover?: {
    /** Texture shown on hover. */
    texture: TextureResource;
    /** Border thickness in logical units. */
    thickness: number;
  };
  /** Grid layout configuration for child panels. */
  layout?: GridLayout,
  /** Child panels nested inside this panel. */
  children?: PanelOptions[]
};

/**
 * Alignment preset options for content inside a panel.
 * Determines where text or numbers are positioned within the panel bounds.
 */
export type AlignOption = "top"
  | "top-left"
  | "top-right"
  | "center"
  | "center-left"
  | "center-right"
  | "bottom"
  | "bottom-left"
  | "bottom-right"

/**
 * Union of all supported panel content components.
 * Each component must also supply an {@link AlignOption} for positioning.
 */
export type PanelContent = (EntityTextValueComponent
  | ConstantTextComponent
  | EntityNumberValueComponent
  | ConstantNumberComponent
  | ContainerListViewComponent
  ) & {
  /** Alignment of the content within the panel. */
  align: AlignOption
}

/**
 * Renders the text value of a named field from an entity.
 */
export type EntityTextValueComponent = {
  /** Discriminant type identifier. */
  type: "entityTextValue"
  /** Name of the entity field to read. */
  name: StringExpression,
  /** Optional entity ID override; defaults to the panel's bound entity. */
  entityId?: StringExpression,
}

/**
 * Renders a constant text string.
 */
export type ConstantTextComponent = {
  /** Discriminant type identifier. */
  type: "constant",
  /** Text string to display. */
  value: StringExpression
}

/**
 * Renders the numeric value of a named field from an entity.
 */
export type EntityNumberValueComponent = {
  /** Discriminant type identifier. */
  type: "entityNumberValue"
  /** Name of the entity field to read. */
  name: StringExpression,
  /** Optional entity ID override; defaults to the panel's bound entity. */
  entityId?: StringExpression,
}

/**
 * Renders a constant numeric value.
 */
export type ConstantNumberComponent = {
  /** Discriminant type identifier. */
  type: "constantNumber",
  /** Numeric value to display. */
  value: NumberExpression
}

/**
 * Renders a scrollable list of child panels, one per entity in a container.
 * Each child panel is created from the panel's template lambda.
 */
export type ContainerListViewComponent = {
  /** Discriminant type identifier. */
  type: "containerListView"
  /** ID of the container whose entities are rendered as list items. */
  containerId: StringExpression
  /** When `true` (default), children stack vertically. When `false`, they stack horizontally. */
  vertical?: boolean
  template:(entity:Entity, index:number) => PanelContent
}

/**
 * Click handler that emits a named action through the runtime.
 */
export type PanelOnClickHandler = {
  /** Discriminant handler type. */
  type: "emitAction",
  /** Name of the action to emit when the panel is clicked. */
  actionName: StringExpression
}

/**
 * Grid layout configuration for arranging child panels in a grid.
 */
export type GridLayout = {
  /** Column definitions; the array length determines the number of columns. */
  columns?: TrackDefinition[];
  /** When `true` (default), children fill left-to-right then wrap down. When `false`, top-to-bottom then wrap right. */
  rowFirst?: boolean;
  /** When `true`, reverses the child placement order within the grid. */
  reverse?: boolean;
  /** Spacing between grid cells. */
  gap?: {
    /** Vertical gap between rows in logical units. */
    row?: NumberExpression;
    /** Horizontal gap between columns in logical units. */
    column?: NumberExpression;
  };
};

/**
 * Definition of a single grid track (column or row) with size constraints and alignment.
 */
export type TrackDefinition = SizeConstraint & {
  /** Content alignment within the track. Defaults to `"start"`. */
  align?: "start" | "end";
};

/**
 * Size constraints applied to a grid track.
 */
export type SizeConstraint = {
  /** Minimum size in logical units; the track will never shrink below this. */
  min?: NumberExpression;
  /** Maximum size in logical units; the track will never grow beyond this. */
  max?: NumberExpression;
  /** Proportional weight for distributing remaining space after minimum sizes are satisfied. */
  weight?: NumberExpression;
};
