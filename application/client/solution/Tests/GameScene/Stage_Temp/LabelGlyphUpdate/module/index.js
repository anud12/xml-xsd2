/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });

  const game = hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: { property: number.of(10) },
  });

  hostApi.ui.window("add-btn", {
    panel: { x: 600, y: 440, width: 100, height: 40 },
  }, [
    hostApi.ui.field("add-label", { entity: game, map: "number", name: "property", fallback: "0" }, font),
  ]);
};
