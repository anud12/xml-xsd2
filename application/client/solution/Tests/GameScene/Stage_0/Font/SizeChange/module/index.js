/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const smallFont = hostApi.ui.setFont({
    file: "fonts/Ubuntu-Regular.ttf",
    size: 12,
    color: "#FFFFFF",
    style: [],
  });
  const largeFont = hostApi.ui.setFont({
    file: "fonts/Ubuntu-Regular.ttf",
    size: 32,
    color: "#FFFFFF",
    style: [],
  });

  hostApi.ui.window("small-window", {
    panel: { width: 200, height: 50, x: 100, y: 100 },
  }, [
    hostApi.ui.text("small-text", "Small", smallFont),
  ]);

  hostApi.ui.window("large-window", {
    panel: { width: 200, height: 60, x: 100, y: 200 },
  }, [
    hostApi.ui.text("large-text", "Large", largeFont),
  ]);
};
