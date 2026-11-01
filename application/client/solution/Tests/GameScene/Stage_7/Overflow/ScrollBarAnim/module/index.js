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

  const panelBg = hostApi.runtime.setAnimation(string.of("panelBg"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("panel_bg.png")}], duration: number.of(1),
  });
  const red = hostApi.runtime.setAnimation(string.of("red"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("item_red.png")}], duration: number.of(1),
  });
  // 2-frame scrollbar animation: cyan (frame 0) -> magenta (frame 1),
  // 2 duration ticks for 2 frames = 1 tick per frame.
  const sbAnim = hostApi.runtime.setAnimation(string.of("sbAnim"), {
    frames: [
      {sprite: hostApi.ui.getSpritePNG("scroll_track.png")},
      {sprite: hostApi.ui.getSpritePNG("scroll_grabber.png")}
    ],
    duration: number.of(2),
    loop: true,
  });

  hostApi.ui.window("sb-anim-host", {
    panel: {
      x: 400, y: 450, width: 120, height: 60, layout: "row",
      overflow: {
        width: "scroll", height: "show", barSize: 8,
        texture: {
          track: {animation: sbAnim, fill: {width: "stretch", height: "stretch"}},
          grabber: {animation: sbAnim, fill: {width: "stretch", height: "stretch"}}
        }
      },
      background: {animation: panelBg, fill: {width: "stretch", height: "stretch"}}
    },
  }, [hostApi.ui.entityList("sb-anim-list", {
    container: inv, vertical: false
  }, (entity) => {
    return [hostApi.ui.window(entity.id + "-sb", {
      panel: {
        width: 40, height: 40,
        background: {animation: red, fill: {width: "stretch", height: "stretch"}}
      }
    }),];
  }),]);
};
