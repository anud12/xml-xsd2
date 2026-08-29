/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.window("center", {
    width: 200,
    height: 200,
    x: -50,
    y: -50,
    background: { name: "texture", duration: 1 },
  }, [])
}
