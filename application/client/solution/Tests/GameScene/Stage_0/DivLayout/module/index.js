/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.panel("div-host", {
    width: 100,
    height: 100,
  }, [
    hostApi.ui.panel("col-div", { layout: "column" }, [
      hostApi.ui.text("div-a", "a"),
      hostApi.ui.text("div-b", "b"),
      hostApi.ui.text("div-c", "c"),
    ]),
    hostApi.ui.panel("row-div", { layout: "row" }, [
      hostApi.ui.text("row-x", "x"),
      hostApi.ui.text("row-y", "y"),
    ]),
  ])
}
