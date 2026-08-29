/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  
  const layerBindings = [
    {
      layer: "center",
      texture: hostApi.ui.getSpritePNG("texture/texture.png"),
    },
  ];

  hostApi.runtime.registerAnimation(string.of("idle"), {
    frames: [
      { sprite: hostApi.ui.spriteMapTIFF("maps/missing.tiff", layerBindings) },
    ],
  });
  
  hostApi.ui.window("characterPanel", {
    width: 10,
    height: 10,
    background: hostApi.ui.getAnimation(string.of("idle"), { duration: number.of(4), loop: true }),
  }, [])
}
