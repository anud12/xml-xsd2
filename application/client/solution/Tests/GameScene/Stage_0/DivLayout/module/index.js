/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF", style: [] });
  hostApi.ui.window("div-host", {
    panel: {
      width: 100,
      height: 100,
    },
  }, [
    hostApi.ui.panel("col-div", { panel: { layout: "column" } }, [
      hostApi.ui.text("div-a", "a", font),
      hostApi.ui.text("div-b", "b", font),
      hostApi.ui.text("div-c", "c", font),
    ]),
    hostApi.ui.panel("row-div", { panel: { layout: "row" } }, [
      hostApi.ui.text("row-x", "x", font),
      hostApi.ui.text("row-y", "y", font),
    ]),
  ])
}
