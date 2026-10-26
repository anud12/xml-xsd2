import { memoize } from "../../util/memoize";
export const getLandSprites = memoize((hostApi) => {
    return [
        hostApi.ui.getSpritePNG("assets/land/land_1.png"),
        hostApi.ui.getSpritePNG("assets/land/land_2.png"),
        hostApi.ui.getSpritePNG("assets/land/land_3.png"),
        hostApi.ui.getSpritePNG("assets/land/land_4.png"),
        hostApi.ui.getSpritePNG("assets/land/land_5.png"),
        hostApi.ui.getSpritePNG("assets/land/land_6.png"),
        hostApi.ui.getSpritePNG("assets/land/land_7.png"),
    ];
});
