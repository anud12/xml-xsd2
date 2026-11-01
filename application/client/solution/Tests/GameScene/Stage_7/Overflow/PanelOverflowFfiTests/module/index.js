/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, number} = hostApi.runtime;

  const itemA = hostApi.runtime.setEntity({id: string.of("item-a")});
  const itemB = hostApi.runtime.setEntity({id: string.of("item-b")});
  const itemC = hostApi.runtime.setEntity({id: string.of("item-c")});
  const itemD = hostApi.runtime.setEntity({id: string.of("item-d")});
  const itemE = hostApi.runtime.setEntity({id: string.of("item-e")});
  const inv = hostApi.runtime.setContainer({
    id: string.of("inventory"),
    entities: [itemA, itemB, itemC, itemD, itemE],
  });

  // Scrollbar textures (required on every overflow declaration): the track is
  // the bar the grabber slides on, the grabber is the draggable part. Each uses a
  // dedicated specific solid color (blue track, red grabber) so the textured bar
  // is visually unambiguous in the references.
  const barTrack = hostApi.runtime.setAnimation(string.of("bar-track"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("scroll_track.png")}], duration: number.of(1),
  });
  const barGrabber = hostApi.runtime.setAnimation(string.of("bar-grabber"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("scroll_grabber.png")}], duration: number.of(1),
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
    panel: {x: 20, y: 100, width: 120, height: 60, layout: "row", overflow: {width: "scroll", height: "show", barSize: 8, texture: {track: {animation: barTrack, fill: {width: "stretch", height: "stretch"}}, grabber: {animation: barGrabber, fill: {width: "stretch", height: "stretch"}}}}},
  }, [
    hostApi.ui.entityList("hscroll-list", {
      container: inv,
      vertical: false
    }, (entity) => [
      hostApi.ui.window(entity.id + "-h", {panel: {width: 40, height: 40}}),
    ]),
  ]);

  // Vertical scroll only: height scrolls, width stays "show".
  hostApi.ui.window("vscroll-host", {
    panel: {x: 20, y: 180, width: 60, height: 100, overflow: {width: "show", height: "scroll", barSize: 8, texture: {track: {animation: barTrack, fill: {width: "stretch", height: "stretch"}}, grabber: {animation: barGrabber, fill: {width: "stretch", height: "stretch"}}}}},
  }, [
    hostApi.ui.entityList("vscroll-list", {container: inv}, (entity) => [
      hostApi.ui.window(entity.id + "-v", {panel: {width: 40, height: 40}}),
    ]),
  ]);

  // Bare string: "scroll" applies to both axes.
  hostApi.ui.window("both-host", {
    panel: {x: 20, y: 300, width: 120, height: 100, layout: "row", overflow: {width: "scroll", height: "scroll", barSize: 8, texture: {track: {animation: barTrack, fill: {width: "stretch", height: "stretch"}}, grabber: {animation: barGrabber, fill: {width: "stretch", height: "stretch"}}}}},
  }, [
    hostApi.ui.entityList("both-list", {container: inv, vertical: false}, (entity) => [
      hostApi.ui.window(entity.id + "-b", {panel: {width: 40, height: 40}}),
    ]),
  ]);

  // Window-level overflow: the window (not the entityList) declares the scroll
  // region; the entityList is a content-sized pass-through (no size/overflow of
  // its own) so the items overflow the window and scroll within it.
  hostApi.ui.window("win-overflow-host", {
    panel: {x: 20, y: 400, width: 120, height: 60, layout: "row", overflow: {width: "scroll", height: "show", barSize: 8, texture: {track: {animation: barTrack, fill: {width: "stretch", height: "stretch"}}, grabber: {animation: barGrabber, fill: {width: "stretch", height: "stretch"}}}}},
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
      panel: {width: 120, height: 60, layout: "row", overflow: {width: "scroll", height: "show", barSize: 8, texture: {track: {animation: barTrack, fill: {width: "stretch", height: "stretch"}}, grabber: {animation: barGrabber, fill: {width: "stretch", height: "stretch"}}}}},
    }, [
      hostApi.ui.window("panel-overflow-item-a", {panel: {width: 40, height: 40}}),
      hostApi.ui.window("panel-overflow-item-b", {panel: {width: 40, height: 40}}),
      hostApi.ui.window("panel-overflow-item-c", {panel: {width: 40, height: 40}}),
    ]),
  ]);
};
