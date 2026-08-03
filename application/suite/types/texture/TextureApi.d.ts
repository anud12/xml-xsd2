import {SpriteMap, MapLayerBinding} from "./SpriteMap";

/**
 * The texture utility API for creating sprite maps and raw texture references.
 */
export type TextureApi = {
  /**
    * Creates a sprite map from a 16-bit integer TIFF file with layer-to-skin bindings.
    * The TIFF must have Photoshop-compatible layer metadata. R/G channels encode UV
    * coordinates (0..mapSize-1), A is per-pixel mask alpha. Skin textures are 8-bit RGBA PNGs.
    */
  spriteMapTIFF: (mapPath: string, layers: MapLayerBinding[]) => SpriteMap;

  /**
   * Returns a PNG resource reference for the given file path.
   */
  png: (path: string) => string;
}