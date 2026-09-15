/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  
  const {string, number} = hostApi.runtime;
  
  hostApi.runtime.registerAnimation(string.of("marker"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("marker.png") }],
    duration: number.of(1),
  });

  hostApi.runtime.setEntity(string.of("node-1"), {
    numberMap: {
      column: number.of(0),
      row: number.of(0),
    },
  });

  // A 10x5 container: the 700x500 plane is split into 10 columns of 70 and 5
  // rows of 100, so each container cell maps to exactly one grid cell.
  hostApi.runtime.setContainer(string.of("grid-1"), {
    entities: [string.of("node-1")],
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

  // A speed-1 move toward (3,2): advances one cell per tick along a diagonal.
  // The shorter axis (y) settles at 2 and holds; the move keeps going until x
  // also reaches its target — the destination (3,2) — at which point it stops.
  hostApi.runtime.registerAction({
    name: string.of("move-to-cursor"),
    apply: (ctx) => {
      
      ctx.moveTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: ctx.args.x,
        y: ctx.args.y,
        speed: 2,
      });
    },
  });


  // Readouts bind node-1's column/row so the rendered labels follow the move.
  hostApi.ui.panel("col", {
    width: 80,
    height: 40,
    x: 10,
    y: 20,
  }, [
    hostApi.ui.field("col-value", { entity: "node-1", map: "number", name: "column", fallback: "0" }),
  ]);

  hostApi.ui.panel("row", {
    width: 80,
    height: 40,
    x: 100,
    y: 20,
  }, [
    hostApi.ui.field("row-value", { entity: "node-1", map: "number", name: "row", fallback: "0" }),
  ]);

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("bar-loop"), {
    frames: [
      {sprite: hostApi.ui.getSpritePNG("frame_2.png")},
      {sprite: hostApi.ui.getSpritePNG("frame_3.png")},
      {sprite: hostApi.ui.getSpritePNG("frame_4.png")},
      {sprite: hostApi.ui.getSpritePNG("frame_5.png")},
    ],
    duration: hostApi.runtime.number.of(5),
    loop: true,
  });
  
  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
      frames: [
        {sprite: hostApi.ui.getSpritePNG("frame_1.png")},
      ],
      duration: hostApi.runtime.number.of(1),
    });
  

  // The 700x500 view of the 10x5 container: each container cell is 70x100, and
  // the view places the entity panel exactly on its cell, re-resolved each tick
  // so the panel tracks the move.
  hostApi.ui.containerView("plane", {
    container: "grid-1",
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
    background: hostApi.ui.getAnimation(string.of("marker")),
    onClick: (ctx) => {
      hostApi.runtime.log("plane click col=" + ctx.cursor.getX() + " row=" + ctx.cursor.getY());
      ctx.emitAction("move-to-cursor", {
        x: ctx.cursor.getX(),
        y: ctx.cursor.getY(),
      });
    },
  }, entity => hostApi.ui.window(entity.id, {}, [
    hostApi.ui.panel(entity.id + "-marker", {
      background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
    }),
  ]))
  
  // const {number, string} = hostApi.runtime;
  // const filter = {
  //   id: id => id.isContainingExactly(string.of("entity_id")),
  // };
  // const key = string.of("key");
  //
  // hostApi.runtime.setEntity(string.of("entity_id"), {
  //   numberMap: {
  //     "key": number.of(0)
  //   },
  //   textMap: {
  //     "isModified": string.of("No")
  //   }
  // })
  //
  // hostApi.runtime.registerEffect({
  //   name: "key-modify-if-par",
  //   prepare: (context, input) => {
  //     const output = context.getEntityBy(filter).get(number.of(0)).flatMap(v => {
  //       return v.getNumber(key)
  //     })
  //       .map(v => v.modulo(number.of(3))
  //         .isEqualTo(number.of(0)));
  //     return output.orElse(hostApi.runtime.condition.of(false));
  //   },
  //   apply: (context, output) => {
  //     output.ifTrue(() => {
  //       context.getEntityBy(filter).get(number.of(0))
  //         .map(v => v.getText(string.of("isModified")).ifPresent(v => {
  //           v.set(string.of("Yes"))
  //         }))
  //     })
  //   }
  // })
  //
  // hostApi.runtime.registerEffect({
  //   name: "repeat",
  //   reoccurAfterMs: (context, executionCount, input, output) => {
  //     return context.getEntityBy(filter)
  //       .get(number.of(0))
  //       .flatMap(elementExpr => elementExpr.getNumber(key))
  //       .isCondition(value => value.isLessOrEqualTo(number.of(20)))
  //       .getOnTrueOrFalse(hostApi.runtime.maybe.of(hostApi.runtime.number.of(1)), hostApi.runtime.maybe.none());
  //   },
  //   prepare: (context, input) => {
  //     return context.emitEvent(string.of("key-modify-if-par"), {})
  //   },
  //   apply: (context, output) => {
  //     context.getEntityBy(filter)
  //       .map(elementExpr => {
  //         elementExpr.getNumber(key).map(v => v.sum(number.of(1)));
  //       })
  //   }
  // })
  //
  // hostApi.runtime.emitEvent("repeat", {});
  //
  // hostApi.runtime.registerAnimation(hostApi.runtime.string.of("hover"), {
  //   frames: [
  //     {sprite: hostApi.ui.getSpritePNG("hover.png")},
  //   ],
  //   duration: hostApi.runtime.number.of(1),
  // });
  // hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
  //   frames: [
  //     {sprite: hostApi.ui.getSpritePNG("frame_1.png")},
  //     {sprite: hostApi.ui.getSpritePNG("frame_2.png")},
  //     {sprite: hostApi.ui.getSpritePNG("frame_3.png")},
  //     {sprite: hostApi.ui.getSpritePNG("frame_4.png")},
  //     {sprite: hostApi.ui.getSpritePNG("frame_5.png")},
  //   ],
  //   duration: hostApi.runtime.number.of(5),
  //   loop: true,
  // });
  // hostApi.runtime.registerAnimation(hostApi.runtime.string.of("textureSlow"), {
  //   frames: [
  //     {sprite: hostApi.ui.getSpritePNG("frame_1.png")},
  //     {sprite: hostApi.ui.getSpritePNG("frame_2.png")},
  //     {sprite: hostApi.ui.getSpritePNG("frame_3.png")},
  //     {sprite: hostApi.ui.getSpritePNG("frame_4.png")},
  //     {sprite: hostApi.ui.getSpritePNG("frame_5.png")},
  //   ],
  //   duration: hostApi.runtime.number.of(30),
  //   loop: true,
  // });
  //
  // hostApi.runtime.registerContainer({
  //   id: "grid-1",
  //   entities: [
  //     hostApi.runtime.string.of("node-1"),
  //     hostApi.runtime.string.of("node-2"),
  //   ],
  //   getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
  //   getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
  //   getSpanX: (entity) => number.of(1),
  //   getSpanY: (entity) => number.of(1),
  //   sizeX: {
  //     value: number.of(10),
  //     outOfBounds: "clamp",
  //   },
  //   sizeY: {
  //     value: number.of(5),
  //     outOfBounds: "clamp",
  //   },
  // });
  //
  // hostApi.runtime.registerAction({
  //   name: string.of("teleport-to-cursor"),
  //   apply: (ctx) => {
  //     ctx.teleportTo({
  //       containerId: string.of("grid-1"),
  //       entityId: string.of("node-1"),
  //       x: ctx.args.x,
  //       y: ctx.args.y,
  //       clamp: true,
  //     });
  //   },
  // });
  //
  // hostApi.ui.panel("ui", {
  //   x: 1,
  //   y: 1,
  //   width: 600,
  //   height: 600,
  //   background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  //   // The panel represents grid-1: the click cursor resolves from the
  //   // container's actual size (sizeX 10, sizeY 5), not the layout tracks.
  //   container: string.of("grid-1"),
  //   layout: {
  //     columns: [{scale: 1}, {scale: 1}, {scale: 1}],
  //     rows: [{scale: 1}, {scale: 1}, {scale: 1}],
  //   },
  //   onClick: (ctx) => {
  //     hostApi.runtime.log("Click")
  //     ctx.emitAction("teleport-to-cursor", {
  //       x: ctx.cursor.getX(),
  //       y: ctx.cursor.getY(),
  //     });
  //   }
  // }, [
  //   hostApi.ui.entityList("ui-view", {container: "grid-1"}, entity => [
  //     hostApi.ui.panel(entity.id, {
  //       width: 10,
  //       height: 10,
  //       background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  //     })
  //   ])
  // ])
  // hostApi.ui.panel("center", {
  //   x: 70,
  //   y: 70,
  //   width: 100,
  //   height: 100,
  //   onHover: {
  //     texture: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover")),
  //     thickness: 5,
  //   },
  //   background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
  // }, [
  //   hostApi.ui.field("centerContent", { entity: "entity_id", map: "number", name: "key", fallback: "0", align: "top" }),
  // ])
  //
  //
  // hostApi.ui.panel("isModifiedPanel", {
  //   x: 250,
  //   y: 100,
  //   width: 100,
  //   height: 100,
  //   onHover: {
  //     texture: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover")),
  //     thickness: 10,
  //   },
  //   background: hostApi.ui.getAnimation(hostApi.runtime.string.of("textureSlow")),
  // }, [
  //   hostApi.ui.field("isModifiedContent", { entity: "entity_id", map: "text", name: "isModified", fallback: "No", align: "center" }),
  // ])
}
