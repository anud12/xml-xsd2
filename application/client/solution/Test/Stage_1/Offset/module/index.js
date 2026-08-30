/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.panel("top", {
    width: 10,
    height: 10,
    x: 495,
    y: 395,
    background: { name: "texture", duration: 1 },
  }, [])
  hostApi.ui.panel("left", {
    width: 10,
    height: 10,
    x: 395,
    y: 495,
    background: { name: "texture", duration: 1 },
  }, [])
  hostApi.ui.panel("bottom", {
    width: 10,
    height: 10,
    x: 495,
    y: 595,
    background: { name: "texture", duration: 1 },
  }, [])
  hostApi.ui.panel("right", {
    width: 10,
    height: 10,
    x: 595,
    y: 495,
    background: { name: "texture", duration: 1 },
  }, [])
}
