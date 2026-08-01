import {SpriteMap, MapLayerBinding} from "./SpriteMap";

/**
 * The texture utility API for creating sprite maps and raw texture references.
 */
export type TextureApi = {
  /**
   * Creates a sprite map from a TIFF file with layer-to-skin bindings.
   * Each pixel in a TIFF layer uses R as U and G as V coordinates into the bound skin PNG.
   */
  spriteMapTIFF: (mapPath: string, layers: MapLayerBinding[]) => SpriteMap;

  /**
   * Returns a PNG resource reference for the given file path.
   */
  png: (path: string) => string;
}