/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, number} = hostApi.runtime;

  const panelBg = hostApi.runtime.setAnimation(string.of("panelBg"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("panel_bg.png")}], duration: number.of(1),
  });
  const red = hostApi.runtime.setAnimation(string.of("red"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("item_red.png")}], duration: number.of(1),
  });

  const child = (id) => hostApi.ui.window(id, {
    panel: {
      width: 60, height: 40,
      background: {animation: red, fill: {width: "stretch", height: "stretch"}}
    }
  });

  // Custom barSize: a horizontal-scroll window whose scrollbar is 20px thick
  // (not the 8px default). Three 60px children (180px) overflow the 120px
  // window, so the horizontal bar is shown at the requested 20px thickness.
  hostApi.ui.window("bar-custom", {
    panel: {
      x: 400, y: 450, width: 120, height: 80, layout: "row",
      overflow: {width: "scroll", height: "show", barSize: 20},
      background: {animation: panelBg, fill: {width: "stretch", height: "stretch"}}
    },
  }, [child("bar-custom-1"), child("bar-custom-2"), child("bar-custom-3")]);

  // Default barSize: identical window using the engine's default thickness
  // (8px), to contrast with the 20px custom bar above.
  hostApi.ui.window("bar-default", {
    panel: {
      x: 560, y: 450, width: 120, height: 80, layout: "row",
      overflow: {width: "scroll", height: "show", barSize: 8},
      background: {animation: panelBg, fill: {width: "stretch", height: "stretch"}}
    },
  }, [child("bar-default-1"), child("bar-default-2"), child("bar-default-3")]);
};
