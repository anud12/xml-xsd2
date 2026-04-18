import {NumberExpression} from "../primitives/numberExpression";
import {BoxApi, BoxOptions} from "./Box";
import {TextureResource} from "../texture/TextureResource";

export type RegisterPanelFunction = (panelOptions: PanelOptions) => {}

export type PanelApi = {
  createBox: (boxOptions: BoxOptions) => BoxApi
}

export type PanelOptions = {
  id: string,
  /**
   * Components can anchor within their cell to control positioning and growth direction:
   *
   * - **0 (edge)**: Anchors to start/top; content grows rightward/downward
   * - **0.5 (center)**: Anchors to center; content grows symmetrically
   * - **1 (edge)**: Anchors to end/bottom; content grows leftward/upward
   */
  anchor: { x: NumberExpression; y: NumberExpression };
  /**
   * Normalised panel point that aligns to anchor (0,0 = panel top-left; 1,1 = panel bottom-right).
   */
  pivot: { x: NumberExpression; y: NumberExpression };
  /**
   * Displacement in logical units after anchor/pivot alignment.
   */
  offset: { x: NumberExpression; y: NumberExpression };
  size: { width: NumberExpression; height: NumberExpression };
  /**
   * Like CSS `flex-grow`. A component with `scale: 2` claims twice as
   * much remaining space as one with `scale: 1`.
   * Defaults to 0 (sized to min, or content if no min).
   */
  scale?: NumberExpression;
  background?: TextureResource;

  children: (panelApi:PanelApi) => void
};