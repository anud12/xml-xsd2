import {SpriteResource} from "./SpriteResource";

export type TextureApi = {
  of: (path: string) => SpriteResource;
  getSpritePNG: (path: string) => SpriteResource;
}