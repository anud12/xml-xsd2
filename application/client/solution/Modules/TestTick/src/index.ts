const entry: ModuleEntrypoint = (hostApi) => {

  const { string, number } = hostApi.runtime;

  const marker = hostApi.runtime.setAnimation(string.of("marker"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("black_pixel.png") }],
    duration: number.of(1),
  });

  const node1 = hostApi.runtime.setEntity({
    id: string.of("node-1"),
    numberMap: {
      column: number.of(0),
      row: number.of(0),
    },
  });

  // A 10x5 container: the 700x500 plane is split into 10 columns of 70 and 5
  // rows of 100, so each container cell maps to exactly one grid cell.
  const grid1 = hostApi.runtime.setContainer({
    id: string.of("grid-1"),
    entities: [node1],
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    getSpanX: (entity) => number.of(10),
    getSpanY: (entity) => number.of(10),
    alignment: "center",
    sizeX: {
      value: number.of(40),
      outOfBounds: "clamp",
    },
    sizeY: {
      value: number.of(40),
      outOfBounds: "clamp",
    },
  });

  // A speed-2 move toward the clicked cell: advances along a diagonal until
  // both axes reach the destination, then stops.
  const moveToCursor = hostApi.runtime.setAction({
    name: string.of("move-to-cursor"),
    apply: (ctx) => {

      ctx.moveTo({
        containerId: grid1,
        entityId: node1,
        x: ctx.args.x,
        y: ctx.args.y,
        speed: 2,
      });
    },
  });


  // Readouts bind node-1's column/row so the rendered labels follow the move.
  hostApi.ui.window("col", {
    panel: {
      width: 80,
      height: 40,
      x: 10,
      y: 20,
    },
  }, [
    hostApi.ui.field("col-value", { entity: node1, map: "number", name: "column", fallback: "0" }),
  ]);

  hostApi.ui.window("row", {
    panel: {
      width: 80,
      height: 40,
      x: 100,
      y: 20,
    },
  }, [
    hostApi.ui.field("row-value", { entity: node1, map: "number", name: "row", fallback: "0" }),
  ]);

  hostApi.runtime.setAnimation(hostApi.runtime.string.of("bar-loop"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
    ],
    duration: hostApi.runtime.number.of(5),
    loop: true,
  });

  const texture = hostApi.runtime.setAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("assets/depot_1.png") },
    ],
    duration: hostApi.runtime.number.of(1),
  });


  // The 700x500 view of the 10x5 container: each container cell is 70x100, and
  // the view places the entity panel exactly on its cell, re-resolved each tick
  // so the panel tracks the move.
  
  const animation = hostApi.runtime.setAnimation(string.of("loading"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("assets/loading_bar.png")}
    ]
  })
  
  // The bar must live under a window (a standalone progress is a no-op): the
  // window carries the 400,400 origin and the bar fills it at local 0,0.
  hostApi.ui.window("progress", {
    panel: {
      x: 400,
      y: 400,
      width: 500,
      height: 30,
    },
  }, [
    hostApi.ui.progress("progress-bar", {
      panel: {
        x: 0,
        y: 0,
        width: 500,
        height: 30,
      },
      progress: {
        value: entity => entity.number_map.get("row").orElse(number.of(0)),
        max: entity => number.of(40),
        entity: node1,
        steps: 100,
        min: entity => number.of(0),
        fill: animation,
        track: marker,
      },
    }),
  ]);
  
  hostApi.ui.containerView("plane", {
    container: grid1,
    panel: {
      width: 500,
      height: 500,
      x: 10,
      y: 80,
      resizable: {
        keepAspectRatio: true
      },
      border: {
        texture: hostApi.ui.getSpritePNG("black_pixel.png")
      },
      background: marker,
      onClick: (ctx) => {
        hostApi.runtime.log("plane click col=" + ctx.cursor.getX() + " row=" + ctx.cursor.getY());
        ctx.emitAction(moveToCursor, {
          x: ctx.cursor.getX(),
          y: ctx.cursor.getY(),
        });
      },
    },
  }, entity => hostApi.ui.window(entity.id, { panel: {} }, [
    hostApi.ui.panel(entity.id + "-marker", {
      panel: {
        background: texture,
      },
    }),
  ]))
};

export default entry;
