/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;
  const buyAction = hostApi.runtime.setAction({
    name: string.of("buy"),
    apply: () => {
      hostApi.runtime.log("___From module buy fired line___")
    }
  });

  // The host window is a plain sized surface (no onClick). It contains a
  // nested panel window "click-panel" that owns the onClick. Clicking the
  // nested panel records the panel's own node id, not the host's.
  hostApi.ui.window("host", {
    panel: {
      width: 220,
      height: 140,
      x: 20,
      y: 20,
    },
  }, [
    hostApi.ui.window("click-panel", {
      panel: {
        width: 90,
        height: 50,
        x: 15,
        y: 15,
        onClick: (ctx) => {
          ctx.emitAction(buyAction);
        },
      },
    }, []),
  ])
}
