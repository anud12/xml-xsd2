import {SpriteResource} from "./SpriteResource";

/**
 * Reference to a TIFF map file whose layers are bound to texture resources.
 *
 * The TIFF must be a 16-bit unsigned integer RGBA image with Photoshop-compatible
 * metadata (layer names stored in the PSD IFD). Each pixel in a layer encodes
 * UV coordinates into its bound skin texture.
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
  /** Path to the 16-bit TIFF map file. Must be RGBA, little-endian, uncompressed,
   * with Photoshop-compatible layer metadata (PSD IFD). */
  map: string;
  /** Bindings that wire each named TIFF layer to its texture resource. */
  layers: MapLayerBinding[];
}

/**
 * Binds a named layer inside a TIFF map to a single 8-bit RGBA PNG skin texture.
 *
 * Each pixel in the bound 16-bit integer layer encodes:
 * - **Red channel**   → U coordinate (0..mapWidth-1, normalized by map width)
 * - **Green channel** → V coordinate (0..mapHeight-1, normalized by map height)
 * - **Alpha channel** → Per-pixel mask alpha (high byte), modulates layer alpha
 *
 * The blue channel is unused.
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
  /** The name of the Photoshop layer as it appears in the TIFF/PSD file. */
  layer: string;
  /** Path to the 8-bit RGBA PNG skin texture this layer samples from. */
  texture: SpriteResource;
}
