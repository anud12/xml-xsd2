/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("top-left", {
    width: 300,
    height: 300,
    background: hostApi.ui.getAnimation(string.of("texture")),
    align: "top-left",
  }, [
    hostApi.ui.text("content", "top-left"),
  ])
}