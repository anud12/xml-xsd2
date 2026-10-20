/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  
  const layerBindings = [
    {
      layer: "center",
      texture: hostApi.ui.getSpritePNG("texture/texture.png"),
    },
  ];

  const idle = hostApi.runtime.setAnimation(string.of("idle"), {
    frames: [
      { sprite: hostApi.ui.spriteMapTIFF("maps/idle_frame1.tiff", layerBindings) },
    ],
    duration: number.of(4), loop: true,
  });
  
  hostApi.ui.window("characterPanel", {
    panel: {
      width: 10,
      height: 10,
      background: { animation: idle, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [])
}
