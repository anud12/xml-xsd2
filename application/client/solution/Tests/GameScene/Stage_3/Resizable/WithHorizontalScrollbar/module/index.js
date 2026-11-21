/**
 * A resizable panel whose horizontal entity list is WIDER than the panel, so a
 * horizontal overflow scroll bar appears. Reproduces the user report: with the
 * list wider than the panel the panel could not be resized horizontally by
 * dragging its left/right border.
 */
/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { string, number } = hostApi.runtime;

  const panelBg = hostApi.runtime.setAnimation(string.of("panelBg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("panel_bg.png") }], duration: number.of(1),
  });
  const item = hostApi.runtime.setAnimation(string.of("item"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("item.png") }], duration: number.of(1),
  });
  const scrollTrack = hostApi.runtime.setAnimation(string.of("scrollTrack"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("scroll_track.png") }], duration: number.of(1),
  });
  const scrollGrabber = hostApi.runtime.setAnimation(string.of("scrollGrabber"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("scroll_grabber.png") }], duration: number.of(1),
  });

  const ents = [];
  for (let i = 0; i < 6; i++) {
    ents.push(hostApi.runtime.setEntity({ id: string.of("ent" + i) }));
  }
  const inv = hostApi.runtime.setContainer({ id: string.of("inv"), entities: ents });

  // A vertical column of 6 items, each 240px wide x 20px tall, inside a
  // 200x100 panel: the items are WIDER than the panel (horizontal overflow) and
  // the column is TALLER than the panel (vertical overflow), so BOTH scroll bars
  // appear. Matches "an entity list bigger than the width".
  hostApi.ui.window("resizable-hscroll", {
    panel: {
      x: 100, y: 100, width: 200, height: 100,
      resizable: { horizontal: true, vertical: true },
      overflow: {
        width: "scroll", height: "scroll", barSize: 8,
        texture: {
          track: { animation: scrollTrack, fill: { width: "stretch", height: "stretch" } },
          grabber: { animation: scrollGrabber, fill: { width: "stretch", height: "stretch" } },
        },
      },
      background: { animation: panelBg, fill: { width: "stretch", height: "stretch" } },
    },
  }, [hostApi.ui.entityList("list", { container: inv }, (entity) => [
    hostApi.ui.window(entity.id, {
      panel: {
        width: 240, height: 20,
        background: { animation: item, fill: { width: "stretch", height: "stretch" } },
      },
    }, []),
  ]),]);
};
