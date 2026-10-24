/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const styledFont = hostApi.ui.setFont({
    file: "fonts/Ubuntu-Regular.ttf",
    size: 24,
    color: "#FF0000",
    style: ["bold"],
  });
  const defaultFont = hostApi.ui.setFont({
    file: "fonts/Ubuntu-Regular.ttf",
    size: 16,
    color: "#FFFFFF",
    style: [],
  });

  hostApi.ui.window("font-window", {
    panel: {
      width: 200,
      height: 50,
      x: 100,
      y: 100,
    },
  }, [
    hostApi.ui.text("font-text", "Hello", styledFont),
  ]);

  hostApi.ui.window("plain-window", {
    panel: {
      width: 200,
      height: 50,
      x: 100,
      y: 200,
    },
  }, [
    hostApi.ui.text("plain-text", "World", defaultFont),
  ]);
}
