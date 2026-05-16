import {TextureResource} from "./TextureResource";

export type TextureApi = {
  of: (path: string) => TextureResource
}