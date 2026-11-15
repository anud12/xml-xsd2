/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;
  const buyAction = hostApi.runtime.setAction({
    name: string.of("buy"),
    apply: () => {
      hostApi.runtime.log("___From module buy fired line___")
    }
  });

  hostApi.ui.window("buy-btn", {
    panel: {
      width: 120,
      height: 60,
      x: 20,
      y: 20,
      onClick: (ctx) => {
        ctx.emitAction(buyAction);
      },
    },
  }, [])
}
