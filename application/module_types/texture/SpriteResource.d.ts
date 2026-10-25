import {SpriteMap} from "./SpriteMap";

/**
 * A reference to a sprite resource used for panel backgrounds and hover textures.
 * Can be a raw PNG path or a composed sprite map driven by a TIFF.
 */
export type SpriteResource = string | SpriteMap;
