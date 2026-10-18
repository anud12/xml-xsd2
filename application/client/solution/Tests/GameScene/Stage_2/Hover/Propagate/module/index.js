/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.setAction({
    name: string.of("hoverProp:enter"),
    apply: () => {
      hostApi.runtime.log("___hover prop enter fired line___");
    }
  });
  hostApi.runtime.setAction({
    name: string.of("hoverProp:exit"),
    apply: () => {
      hostApi.runtime.log("___hover prop exit fired line___");
    }
  });
  // The hover emitAction reference is the base action name; the engine appends
  // ":enter"/":exit" when firing, so the handle must name the base.
  const hoverProp = hostApi.runtime.setAction({
    name: string.of("hoverProp"),
    apply: () => {
    }
  });
  const texture = hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.window("hoverParent", {
    panel: {
      width: 100,
      height: 100,
      background: texture,
      onHover: {
        emitAction: hoverProp,
      },
    },
  }, [
    hostApi.ui.window("inner", {
      panel: {
        width: 30,
        height: 30,
        x: 20,
        y: 20,
      },
    }, [])
  ])
}
