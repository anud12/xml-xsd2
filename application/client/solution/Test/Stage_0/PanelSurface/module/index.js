/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.panel("bare", {}, [
    hostApi.ui.text("bare-a", "a"),
    hostApi.ui.text("bare-b", "b"),
  ]);
}
