/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.window("bottom", {
    width: 300,
    height: 300,
    background: hostApi.ui.getAnimation(string.of("texture"), { duration: number.of(1) }),
    align: "bottom",
  }, [
    hostApi.ui.text("content", "bottom"),
  ])
}
