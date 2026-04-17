import {TextureResource} from "../TextureResource";
import {NumberExpression} from "../primitives/numberExpression";

export type TrackDefinition = SizeConstraint & {
  align?: "start" | "end";             // default: "start" — content alignment within track
};
export type BoxApi = {

}

export type SizeConstraint = {
  min?: NumberExpression;    // Never sized below this (logical units)
  max?: NumberExpression;    // Never sized above this (logical units)
  scale?: NumberExpression;  // Flex weight; claims remaining space after min
};

export type ChildSize = {
  width?: SizeConstraint;
  height?: SizeConstraint;
  anchor?: { x?: NumberExpression; y?: NumberExpression };
};

export type GridLayout = {
  columns: TrackDefinition[];          // Column definitions; count = num columns
  rowFirst?: boolean;                  // true (default): left→right, wrap; false: top→bottom
  reverse?: boolean;                   // false (default): reverse child placement order
  gap?: { row?: NumberExpression; column?: NumberExpression };  // Cell spacing
};

type BoxOptions = {
  size?: ChildSize;              // Size hint for parent layout
  layout?: GridLayout;           // Grid layout config; omit for non-layout box
  background?: TextureResource;  // Optional background texture (see rendering.md)
  border?: TextureResource;      // Optional border texture (see rendering.md)
};