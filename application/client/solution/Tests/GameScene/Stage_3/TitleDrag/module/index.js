/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.window("titled", {
    panel: {
      x: 100,
      y: 100,
      width: 200,
      height: 120,
      title: "My Window",
    },
  });
  hostApi.ui.window("titled-resizable", {
    panel: {
      x: 400,
      y: 100,
      width: 200,
      height: 120,
      title: "Resizable",
      resizable: true,
    },
  });
}
