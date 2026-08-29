/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  // Centered: top-left at the viewport center (500, 500).
  hostApi.ui.window("panel", {
    width: 100,
    height: 100,
    background: { name: "texture", duration: 1 },
  }, [])
  // Overlays the top-left quarter of panel — the region under the overlay
  // must show the overlay's background, not the panel's.
  hostApi.ui.window("overlay", {
    width: 50,
    height: 50,
    x: 500,
    y: 500,
    background: { name: "texture", duration: 1 },
  }, [])
}
