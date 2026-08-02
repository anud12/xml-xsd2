/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const layerBindings = [
    {
      layer: "border",
      texture: hostApi.ui.getSpritePNG("skins/border_top.png"),
    },
    {
      layer: "center",
      texture: hostApi.ui.getSpritePNG("skins/texture.png"),
    },
  ];

  hostApi.runtime.registerAnimation(string.of("idle"), {
    frames: [
      { sprite: hostApi.ui.spriteMapTIFF("maps/idle_frame1.tiff", layerBindings) },
    ],
  });

  hostApi.ui.registerPanel({
    id: "characterPanel",
    size: {
      height: number.of(10),
      width: number.of(10),
    },
    offset: {
      top: number.of(50),
      bottom: number.of(50),
      left: number.of(50),
      right: number.of(50),
    },
    background: hostApi.ui.getAnimation(string.of("idle"), { duration: number.of(4), loop: true }),
  });
}