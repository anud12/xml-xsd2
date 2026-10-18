/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("bare", { panel: { x: 0, y: 0 } }, [
    hostApi.ui.text("bare-a", "a"),
    hostApi.ui.text("bare-b", "b"),
  ]);
}
