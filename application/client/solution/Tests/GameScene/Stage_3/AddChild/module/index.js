/** @type {ModuleEntrypoint} */
export default (hostApi) => {
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
  win.addChild(hostApi.ui.text("early", "early"));
  const button = hostApi.ui.window("button", {
    panel: {
      x: 0,
      y: 80,
      width: 60,
      height: 20,
      onClick: () => {
        // Appended at runtime (after the first paint): the next tick's
        // snapshot must carry it.
        win.addChild(hostApi.ui.text("late", "late"));
      },
    },
  });
  win.addChild(button);
}
