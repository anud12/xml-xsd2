/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;
  hostApi.runtime.registerAction({
    name: string.of("stageAction"),
    apply: () => {
        hostApi.runtime.log("___From module stageAction fired line___")
    }
  });
  hostApi.runtime.registerAction({
    name: string.of("childAction"),
    apply: () => {
        hostApi.runtime.log("___From module childAction fired line___")
    }
  });

  hostApi.ui.panel("parent", {
    width: 100,
    height: 100,
    onClick: (ctx) => {
      ctx.emitAction("stageAction");
    },
  }, [
    hostApi.ui.panel("child", {
      width: 10,
      height: 10,
      x: 10,
      y: 10,
      onClick: (ctx) => {
        ctx.emitAction("childAction");
      },
    }, [])
  ])
}
