/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const greenFont = hostApi.ui.setFont({
    file: "fonts/Ubuntu-Regular.ttf",
    size: 16,
    color: "#00FF00",
    style: [],
  });
  const blueFont = hostApi.ui.setFont({
    file: "fonts/Ubuntu-Regular.ttf",
    size: 16,
    color: "#0000FF",
    style: [],
  });

  hostApi.ui.window("green-window", {
    panel: { width: 200, height: 50, x: 100, y: 100 },
  }, [
    hostApi.ui.text("green-text", "Green", greenFont),
  ]);

  hostApi.ui.window("blue-window", {
    panel: { width: 200, height: 50, x: 100, y: 200 },
  }, [
    hostApi.ui.text("blue-text", "Blue", blueFont),
  ]);
};
