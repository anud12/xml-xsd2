/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });
  hostApi.ui.window("center", {
    panel: {
      width: 100,
      height: 100,
      background: { animation: texture, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [])
  hostApi.ui.window("anchor-parent", { panel: { x: 0, y: 0, width: 1000, height: 1000, anchorChild: "center" } }, [
    hostApi.ui.window("anchor-first", { panel: { width: 100, height: 100, anchor: "bottom-right" } }, []),
    hostApi.ui.window("anchor-second", { panel: { x: 23, y: 34, width: 100, height: 100 } }, []),
  ]);
  hostApi.ui.window("anchor-xy-parent", { panel: { x: 0, y: 0, width: 1000, height: 1000, anchorChild: "center" } }, [
    hostApi.ui.window("anchor-xy", { panel: { x: 12, y: 34, width: 100, height: 100 } }, []),
  ]);
  hostApi.ui.setScreen({ screen: { anchorChild: "center" } }, [
    hostApi.ui.window("screen-anchor-first", { panel: { width: 100, height: 100 } }, []),
  ]);
}
