/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAction({
    name: string.of("hoverProp:enter"),
    apply: () => {
      hostApi.runtime.log("___hover prop enter fired line___");
    }
  });
  hostApi.runtime.registerAction({
    name: string.of("hoverProp:exit"),
    apply: () => {
      hostApi.runtime.log("___hover prop exit fired line___");
    }
  });
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("hoverParent", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(string.of("texture")),
    onHover: {
      emitAction: "hoverProp",
    },
  }, [
    hostApi.ui.panel("inner", {
      width: 30,
      height: 30,
      x: 20,
      y: 20,
    }, [])
  ])
}