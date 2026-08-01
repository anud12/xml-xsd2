import {SpriteResource} from "./SpriteResource";

/**
 * Reference to a TIFF map file whose layers are bound to texture resources.
 * The map defines a 1:1 pixel composition: each pixel in a TIFF layer
 * encodes UV coordinates (R=U, G=V) into its bound skin texture.
 *
 * @example
 * ```js
 * hostApi.ui.spriteMapTIFF("maps/idle_frame1.tiff", [
 *   { layer: "eyes", texture: hostApi.ui.getSpritePNG("skins/eyes_blue.png") },
 *   { layer: "body", texture: hostApi.ui.getSpritePNG("skins/body.png") },
 * ])
 * ```
 */
export type SpriteMap = {
  /** Path to the TIFF map file containing named layers. */
  map: string;
  /** Bindings that wire each named TIFF layer to its texture resource. */
  layers: MapLayerBinding[];
}

/**
 * Binds a named layer inside a TIFF map to a single texture resource.
 *
 * Each pixel in the bound layer encodes:
 * - **Red channel**   → U coordinate (horizontal) into the texture
 * - **Green channel** → V coordinate (vertical) into the texture
 *
 * The blue and alpha channels are currently unused.
 *
 * @example
 * ```js
 * {
 *   layer: "eyes",
 *   texture: hostApi.ui.getSpritePNG("skins/eyes_blue.png"),
 * }
 * ```
 */
export type MapLayerBinding = {
  /** The name of the layer as it appears in the TIFF file. */
  layer: string;
  /** The skin PNG this layer samples from using its R/G UV coordinates. */
  texture: SpriteResource;
}
