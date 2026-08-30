/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.panel("default", {
    width: 120,
    height: 120,
    border: { texture: "border.png" },
  });
}
