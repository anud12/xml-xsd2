/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("bottom", {
    panel: {
      width: 300,
      height: 300,
      background: hostApi.ui.getAnimation(string.of("texture")),
      align: "bottom",
    },
  }, [
    hostApi.ui.text("content", "bottom"),
  ])
}
