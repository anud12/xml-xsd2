/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  // Centered: top-left at the viewport center (500, 500).
  hostApi.ui.panel("panel", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
  }, [])
  // Covers the top-left quarter of panel — panel must still pass.
  hostApi.ui.panel("overlay", {
    width: 50,
    height: 50,
    x: 500,
    y: 500,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture"), { duration: hostApi.runtime.number.of(1) }),
  }, [])
  // No background: used by the negative "window without background" case.
  hostApi.ui.panel("plain", {
    width: 50,
    height: 50,
    x: 20,
    y: 20,
  }, [])
}
