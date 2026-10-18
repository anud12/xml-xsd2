/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("top-left", {
    panel: {
      width: 300,
      height: 300,
      background: texture,
      align: "top-left",
    },
  }, [
    hostApi.ui.text("content", "top-left"),
  ])
}
