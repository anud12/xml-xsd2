/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.world.sectorGrid(string.of("cave"));

  // A single-cell sector "room-a" at [0,0]. It declares NO openings, so it
  // can never link to a neighbour: a doorway on the other side of the shared
  // wall is not enough to form a portal.
  hostApi.runtime.setContainer(string.of("room-a"), {
    entities: [],
    sector: {
      grid: string.of("cave"),
      footprint: [[0, 0]],
      at: [0, 0],
      openings: [],
    },
  });

  // The action the test fires: it delegates to an effect.
  hostApi.runtime.registerAction({
    name: string.of("build-b"),
    apply: (ctx) => { ctx.emitEffect("create-b", {}); },
  });

  // The effect: creates "room-b" at (1,0), directly east of A, with a WEST
  // opening facing A. Because A has no opening facing back, this opening is
  // unlinked: a red dead-end shaft on the A-B wall, and no portal forms.
  hostApi.runtime.registerEffect({
    name: "create-b",
    apply: () => {
      hostApi.runtime.setContainer(string.of("room-b"), {
        entities: [],
        sector: {
          grid: string.of("cave"),
          footprint: [[0, 0]],
          at: [1, 0],
          openings: [
            { cell: [0, 0], side: "W", start: number.of(0), length: number.of(1) },
          ],
        },
      });
    },
  });

  hostApi.runtime.registerAnimation(string.of("viewbg"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("viewbg.png") }],
    duration: number.of(1),
  });
  hostApi.runtime.registerAnimation(string.of("cell"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("cell.png") }],
    duration: number.of(1),
  });

  hostApi.ui.sectorGrid("caveview", {
    grid: "cave",
    cellSize: 40,
    cellGap: 8,
    width: 700,
    height: 400,
    x: 200,
    y: 200,
    background: hostApi.ui.getAnimation(string.of("viewbg")),
  }, (item) => hostApi.ui.window(item.id, {
    background: hostApi.ui.getAnimation(string.of("cell")),
  }));
};
