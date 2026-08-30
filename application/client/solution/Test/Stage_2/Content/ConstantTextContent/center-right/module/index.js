/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("center-right", {
    width: 300,
    height: 300,
    background: hostApi.ui.getAnimation(string.of("texture")),
    align: "center-right",
  }, [
    hostApi.ui.text("content", "center-right"),
  ])
}