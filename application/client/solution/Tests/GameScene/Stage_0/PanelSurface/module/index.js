/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
  hostApi.ui.window("bare", { panel: { x: 0, y: 0 } }, [
    hostApi.ui.text("bare-a", "a", font),
    hostApi.ui.text("bare-b", "b", font),
  ]);
}
