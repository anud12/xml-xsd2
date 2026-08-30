/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.registerAction({
    name: string.of("parentHover:enter"),
    apply: () => {
      hostApi.runtime.log("___parent hover enter fired line___");
    }
  });
  hostApi.runtime.registerAction({
    name: string.of("parentHover:exit"),
    apply: () => {
      hostApi.runtime.log("___parent hover exit fired line___");
    }
  });
  hostApi.runtime.registerAction({
    name: string.of("childHover:enter"),
    apply: () => {
      hostApi.runtime.log("___child hover enter fired line___");
    }
  });
  hostApi.runtime.registerAction({
    name: string.of("childHover:exit"),
    apply: () => {
      hostApi.runtime.log("___child hover exit fired line___");
    }
  });
  hostApi.runtime.registerAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
  });
  hostApi.ui.panel("parent", {
    width: 100,
    height: 100,
    background: hostApi.ui.getAnimation(string.of("texture"), { duration: number.of(1) }),
    onHover: {
      emitAction: "parentHover",
    },
  }, [
    hostApi.ui.panel("child", {
      width: 20,
      height: 20,
      x: 40,
      y: 40,
      onHover: {
        emitAction: "childHover",
        stopPropagation: true,
      },
    }, [])
  ])
}
