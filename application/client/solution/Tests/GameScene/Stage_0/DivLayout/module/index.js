/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("div-host", {
    panel: {
      width: 100,
      height: 100,
    },
  }, [
    hostApi.ui.panel("col-div", { panel: { layout: "column" } }, [
      hostApi.ui.text("div-a", "a"),
      hostApi.ui.text("div-b", "b"),
      hostApi.ui.text("div-c", "c"),
    ]),
    hostApi.ui.panel("row-div", { panel: { layout: "row" } }, [
      hostApi.ui.text("row-x", "x"),
      hostApi.ui.text("row-y", "y"),
    ]),
  ])
}
