/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.panel("aspect", {
    width: 200,
    height: 100,
    x: 100,
    y: 100,
    resizable: { keepAspectRatio: true },
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  }, []);
}
