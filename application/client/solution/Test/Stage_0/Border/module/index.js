/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  hostApi.ui.panel("bordered", {
    width: 120,
    height: 120,
    background: "background.png",
    border: { width: 3, texture: "border.png" },
  });
}
