/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;

  const itemA = hostApi.runtime.setEntity({id: string.of("item-a")});
  const itemB = hostApi.runtime.setEntity({id: string.of("item-b")});
  const itemC = hostApi.runtime.setEntity({id: string.of("item-c")});
  const itemD = hostApi.runtime.setEntity({id: string.of("item-d")});
  const itemE = hostApi.runtime.setEntity({id: string.of("item-e")});
  const inv = hostApi.runtime.setContainer({
    id: string.of("inventory"),
    entities: [itemA, itemB, itemC, itemD, itemE],
  });

  // Default: no overflow option => "show" on both axes (no scroll wrapper).
  hostApi.ui.window("show-host", {
    panel: {x: 20, y: 20, width: 120, height: 60},
  }, [
    hostApi.ui.entityList("show-list", {container: inv, vertical: false}, (entity) => [
      hostApi.ui.window(entity.id + "-s", {panel: {width: 40, height: 40}}),
    ]),
  ]);

  // Horizontal scroll only: width scrolls, height stays "show".
  hostApi.ui.window("hscroll-host", {
    panel: {x: 20, y: 100, width: 120, height: 60},
  }, [
    hostApi.ui.entityList("hscroll-list", {
      container: inv,
      vertical: false,
      panel: {overflow: {width: "scroll"}}
    }, (entity) => [
      hostApi.ui.window(entity.id + "-h", {panel: {width: 40, height: 40}}),
    ]),
  ]);

  // Vertical scroll only: height scrolls, width stays "show".
  hostApi.ui.window("vscroll-host", {
    panel: {x: 20, y: 180, width: 60, height: 100},
  }, [
    hostApi.ui.entityList("vscroll-list", {container: inv, panel: {overflow: {height: "scroll"}}}, (entity) => [
      hostApi.ui.window(entity.id + "-v", {panel: {width: 40, height: 40}}),
    ]),
  ]);

  // Bare string: "scroll" applies to both axes.
  hostApi.ui.window("both-host", {
    panel: {x: 20, y: 300, width: 120, height: 100},
  }, [
    hostApi.ui.entityList("both-list", {container: inv, vertical: false, panel: {overflow: "scroll"}}, (entity) => [
      hostApi.ui.window(entity.id + "-b", {panel: {width: 40, height: 40}}),
    ]),
  ]);

  // Window-level overflow: the window (not the entityList) declares the scroll
  // region; the entityList is a content-sized pass-through (no size/overflow of
  // its own) so the items overflow the window and scroll within it.
  hostApi.ui.window("win-overflow-host", {
    panel: {x: 20, y: 400, width: 120, height: 60, layout: "row", overflow: {width: "scroll"}},
  }, [
    hostApi.ui.entityList("win-overflow-list", {container: inv, vertical: false}, (entity) => [
      hostApi.ui.window(entity.id + "-w", {panel: {width: 40, height: 40}}),
    ]),
  ]);

  // Plain panel (a division) with overflow: the same scroll behavior applies to
  // any flow-hosting component that carries a panel, not just window/entityList.
  hostApi.ui.window("panel-overflow-host", {
    panel: {x: 20, y: 480, width: 120, height: 60},
  }, [
    hostApi.ui.panel("panel-overflow", {
      panel: {width: 120, height: 60, layout: "row", overflow: {width: "scroll"}},
    }, [
      hostApi.ui.window("panel-overflow-item-a", {panel: {width: 40, height: 40}}),
      hostApi.ui.window("panel-overflow-item-b", {panel: {width: 40, height: 40}}),
      hostApi.ui.window("panel-overflow-item-c", {panel: {width: 40, height: 40}}),
    ]),
  ]);
};
