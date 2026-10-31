/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, number} = hostApi.runtime;

  const itemA = hostApi.runtime.setEntity({id: string.of("item-a")});
  const itemB = hostApi.runtime.setEntity({id: string.of("item-b")});
  const itemC = hostApi.runtime.setEntity({id: string.of("item-c")});
  const itemD = hostApi.runtime.setEntity({id: string.of("item-d")});
  const itemE = hostApi.runtime.setEntity({id: string.of("item-e")});
  const inv = hostApi.runtime.setContainer({
    id: string.of("inventory"), entities: [itemA, itemB, itemC, itemD, itemE],
  });

  const red = hostApi.runtime.setAnimation(string.of("red"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("item_red.png")}], duration: number.of(1),
  });
  const green = hostApi.runtime.setAnimation(string.of("green"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("item_green.png")}], duration: number.of(1),
  });
  const blue = hostApi.runtime.setAnimation(string.of("blue"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("item_blue.png")}], duration: number.of(1),
  });
  const panelBg = hostApi.runtime.setAnimation(string.of("panelBg"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("panel_bg.png")}], duration: number.of(1),
  });

  // Horizontal scroll list: 5 x 40px items = 200px wide inside a 120px-wide
  // panel, so the right-hand items are clipped until the user scrolls right.
  // Positioned in the clear lower-right so it never covers the coreUI
  // settings/actions/reload buttons or the actions window.
  hostApi.ui.window("hscroll-host", {
    panel: {
      x: 400,
      y: 450,
      width: 120,
      height: 60,
      background: {animation: panelBg, fill: {width: "stretch", height: "stretch"}}
    },
  }, [hostApi.ui.entityList("hscroll-list", {
    container: inv, vertical: false, panel: {overflow: {width: "scroll"}, width: 100, height: 30}
  }, (entity) => {
    const tex = entity.id.endsWith("-a") ? red : entity.id.endsWith("-b") ? green : blue;
    return [hostApi.ui.window(entity.id + "-h", {
      panel: {
        width: 40, height: 40, background: {animation: tex, fill: {width: "stretch", height: "stretch"}}
      }
    }),];
  }),]);

  // Window-level scroll: the window itself carries overflow {width: scroll};
  // the entityList is a pass-through (no size, no overflow). The 5 x 40px
  // items (200px) overflow the 140px-wide window, so the window's own scroll
  // wrapper must show a horizontal scrollbar.
  hostApi.ui.window("win-scroll-host", {
    panel: {
      x: 600, y: 450, width: 140, height: 70, layout: "row",
      overflow: {width: "scroll"},
      background: {animation: panelBg, fill: {width: "stretch", height: "stretch"}}
    },
  }, [hostApi.ui.entityList("win-scroll-list", {
    container: inv, vertical: false
  }, (entity) => {
    const tex = entity.id.endsWith("-a") ? red : entity.id.endsWith("-b") ? green : blue;
    return [hostApi.ui.window(entity.id + "-w", {
      panel: {
        width: 40, height: 40, background: {animation: tex, fill: {width: "stretch", height: "stretch"}}
      }
    }),];
  }),]);

  // Vertical scroll list: 5 x 40px items = 200px tall inside a 120px-tall
  // panel, so the bottom items are clipped until the user scrolls down.
  hostApi.ui.window("vscroll-host", {
    panel: {
      x: 400,
      y: 560,
      width: 60,
      height: 120,
      background: {animation: panelBg, fill: {width: "stretch", height: "stretch"}}
    },
  }, [hostApi.ui.entityList("vscroll-list", {
    container: inv, panel: {overflow: {height: "scroll"}, width: 40, height: 100}
  }, (entity) => {
    const tex = entity.id.endsWith("-a") ? red : entity.id.endsWith("-b") ? green : blue;
    return [hostApi.ui.window(entity.id + "-v", {
      panel: {
        width: 40, height: 40, background: {animation: tex, fill: {width: "stretch", height: "stretch"}}
      }
    }),];
  }),]);
};
