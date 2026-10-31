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
};
