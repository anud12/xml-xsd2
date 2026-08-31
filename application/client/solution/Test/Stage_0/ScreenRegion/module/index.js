/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  // Centered: top-left at the viewport center (500, 500).
  hostApi.ui.panel("panel", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  }, [])
  // Overlays the top-left quarter of panel — the region under the overlay
  // must show the overlay's background, not the panel's.
  hostApi.ui.panel("overlay", {
    width: 50,
    height: 50,
    x: 500,
    y: 500,
    background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  }, [])
}