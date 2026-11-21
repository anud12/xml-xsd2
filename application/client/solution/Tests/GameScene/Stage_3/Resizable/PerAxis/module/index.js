/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });

  // Two rows of 200x100 (2:1) windows. Only the enabled axes and the
  // keepAspectRatio flag differ:
  //   row 1 (y:100)  -> keepAspectRatio: true
  //   row 2 (y:300)  -> no keepAspectRatio (free resize)
  // and each column pins one axis configuration:
  //   horizontal-only -> width resizes, height stays fixed
  //   vertical-only   -> height resizes, width stays fixed
  //   both            -> ratio locked (row 1) vs free (row 2)

  // --- Row 1: keepAspectRatio: true ---
  hostApi.ui.window("resizable-h", {
    panel: {
      width: 200,
      height: 100,
      x: 100,
      y: 100,
      resizable: { horizontal: true, vertical: false, keepAspectRatio: true },
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, []);
  hostApi.ui.window("resizable-v", {
    panel: {
      width: 200,
      height: 100,
      x: 400,
      y: 100,
      resizable: { vertical: true, horizontal: false, keepAspectRatio: true },
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, []);
  hostApi.ui.window("resizable-both", {
    panel: {
      width: 200,
      height: 100,
      x: 700,
      y: 100,
      resizable: { horizontal: true, vertical: true, keepAspectRatio: true },
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, []);

  // --- Row 2: free resize (no keepAspectRatio) ---
  hostApi.ui.window("resizable-h-free", {
    panel: {
      width: 200,
      height: 100,
      x: 100,
      y: 300,
      resizable: { horizontal: true, vertical: false },
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, []);
  hostApi.ui.window("resizable-v-free", {
    panel: {
      width: 200,
      height: 100,
      x: 400,
      y: 300,
      resizable: { vertical: true, horizontal: false },
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, []);
  hostApi.ui.window("resizable-both-free", {
    panel: {
      width: 200,
      height: 100,
      x: 700,
      y: 300,
      resizable: { horizontal: true, vertical: true },
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, []);
}
