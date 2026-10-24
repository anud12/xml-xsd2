/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
  const {number, string} = hostApi.runtime;
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("panel", {
    panel: {
      width: 100,
      height: 100,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [
    hostApi.ui.text("panel-content", "Content", font),
  ])
}
