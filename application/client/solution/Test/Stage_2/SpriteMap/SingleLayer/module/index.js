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
      { sprite: hostApi.ui.spriteMapTIFF("maps/idle_frame1.tiff", layerBindings) },
    ],
    duration: number.of(4), loop: true,
  });
  
  hostApi.ui.panel("characterPanel", {
    width: 10,
    height: 10,
    background: hostApi.ui.getAnimation(string.of("idle")),
  }, [])
}