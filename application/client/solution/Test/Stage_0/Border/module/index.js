/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("background"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("background.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("border"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("border.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("bordered", {
    width: 120,
    height: 120,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("background")),
    border: { width: 3, texture: hostApi.ui.getAnimation(hostApi.runtime.string.of("border")) },
  });
}