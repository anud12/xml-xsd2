/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;
  hostApi.runtime.setAction({
    name: string.of("parentHover:enter"),
    apply: () => {
      hostApi.runtime.log("___parent hover enter fired line___");
    }
  });
  hostApi.runtime.setAction({
    name: string.of("parentHover:exit"),
    apply: () => {
      hostApi.runtime.log("___parent hover exit fired line___");
    }
  });
  hostApi.runtime.setAction({
    name: string.of("childHover:enter"),
    apply: () => {
      hostApi.runtime.log("___child hover enter fired line___");
    }
  });
  hostApi.runtime.setAction({
    name: string.of("childHover:exit"),
    apply: () => {
      hostApi.runtime.log("___child hover exit fired line___");
    }
  });
  const parentHover = hostApi.runtime.setAction({
    name: string.of("parentHover"),
    apply: () => {
      hostApi.runtime.log("___parentHover fired line___");
    }
  });
  const childHover = hostApi.runtime.setAction({
    name: string.of("childHover"),
    apply: () => {
      hostApi.runtime.log("___childHover fired line___");
    }
  });
  hostApi.runtime.setAnimation(string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("texture.png") },
    ],
    duration: number.of(1),
  });
  hostApi.ui.panel("parent", {
    panel: {
      width: 100,
      height: 100,
      background: hostApi.ui.getAnimation(string.of("texture")),
      onHover: {
        emitAction: parentHover,
      },
    },
  }, [
    hostApi.ui.panel("child", {
      panel: {
        width: 20,
        height: 20,
        x: 40,
        y: 40,
        onHover: {
          emitAction: childHover,
          stopPropagation: true,
        },
      },
    }, [])
  ])
}
