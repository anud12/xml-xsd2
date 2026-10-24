/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF", style: [] });
  const {number, string} = hostApi.runtime;
  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.window("bottom-left", {
    panel: {
      width: 300,
      height: 300,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
      align: "bottom-left",
    },
  }, [
    hostApi.ui.text("content", "bottom-left", font),
  ])
}
