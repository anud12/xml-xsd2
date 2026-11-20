/**
 * A resizable panel that ALSO has a vertical overflow scroll bar. The scroll
 * bar sits flush on the window's right edge — exactly over the window's
 * edge/corner resize hit zone — and (being a Stop-filter control) would swallow
 * the corner/edge press, blocking the resize. Six 40px children (240px)
 * overflow the 200px panel so the vertical bar is shown; the test proves the
 * panel can still be resized from its corner despite the bar being in the way.
 */
/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, number} = hostApi.runtime;

  const panelBg = hostApi.runtime.setAnimation(string.of("panelBg"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("panel_bg.png")}], duration: number.of(1),
  });
  const item = hostApi.runtime.setAnimation(string.of("item"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("item.png")}], duration: number.of(1),
  });
  // Scrollbar track / grabber textures (required by the overflow option).
  const scrollTrack = hostApi.runtime.setAnimation(string.of("scrollTrack"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("scroll_track.png")}], duration: number.of(1),
  });
  const scrollGrabber = hostApi.runtime.setAnimation(string.of("scrollGrabber"), {
    frames: [{sprite: hostApi.ui.getSpritePNG("scroll_grabber.png")}], duration: number.of(1),
  });

  const child = (id) => hostApi.ui.window(id, {
    panel: {
      width: 192, height: 40,
      background: {animation: item, fill: {width: "stretch", height: "stretch"}},
    },
  }, []);

  hostApi.ui.window("resizable-scroll", {
    panel: {
      x: 100, y: 100, width: 200, height: 200,
      layout: "column",
      resizable: true,
      overflow: {
        width: "show", height: "scroll", barSize: 8,
        texture: {
          track: {animation: scrollTrack, fill: {width: "stretch", height: "stretch"}},
          grabber: {animation: scrollGrabber, fill: {width: "stretch", height: "stretch"}},
        },
      },
      background: {animation: panelBg, fill: {width: "stretch", height: "stretch"}},
    },
  }, [child("s1"), child("s2"), child("s3"), child("s4"), child("s5"), child("s6")]);
};
