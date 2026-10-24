/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });
  const win = hostApi.ui.window("add-child", {
    panel: {
      x: 100,
      y: 100,
      width: 200,
      height: 120,
    },
  });
  // Appended to the window handle after creation (not via the children
  // parameter): present in the first snapshot.
  win.addChild(hostApi.ui.text("early", "early", font));
  const button = hostApi.ui.window("button", {
    panel: {
      x: 0,
      y: 80,
      width: 60,
      height: 20,
      onClick: () => {
        // Appended at runtime (after the first paint): the next tick's
        // snapshot must carry it.
        win.addChild(hostApi.ui.text("late", "late", font));
      },
    },
  });
  win.addChild(button);
}
