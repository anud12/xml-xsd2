import {HostApi} from "module_types/HostApi";
import {SpriteResource} from "module_types/texture/SpriteResource";
import {memoize} from "../../util/memoize";

export const getLandSprites = memoize((hostApi: HostApi): SpriteResource[] => {
  return [
    hostApi.ui.getSpritePNG("assets/land/land_1.png"),
    hostApi.ui.getSpritePNG("assets/land/land_2.png"),
    hostApi.ui.getSpritePNG("assets/land/land_3.png"),
    hostApi.ui.getSpritePNG("assets/land/land_5.png"),
    hostApi.ui.getSpritePNG("assets/land/land_6.png"),
    hostApi.ui.getSpritePNG("assets/land/land_7.png"),
  ];
});
