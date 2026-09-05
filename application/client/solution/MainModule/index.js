// /** @type {ModuleEntrypoint} */
// export default (hostApi) => {
//   const {number, string} = hostApi.runtime;
//   const filter = {
//     id: id => id.isContainingExactly(string.of("entity_id")),
//   };
//   const key = string.of("key");
//  
//   hostApi.runtime.setEntity(string.of("entity_id"), {
//     numberMap: {
//       "key": number.of(0)
//     },
//     textMap: {
//       "isModified": string.of("No")
//     }
//   })
//
//   hostApi.runtime.registerEffect({
//     name: "key-modify-if-par",
//     prepare: (context, input) => {
//       const output = context.getEntityBy(filter).get(number.of(0)).flatMap(v => {
//         return v.getNumber(key)
//       })
//         .map(v => v.modulo(number.of(3))
//           .isEqualTo(number.of(0)));
//       return output.orElse(hostApi.runtime.condition.of(false));
//     },
//     apply:(context, output) => {
//       output.ifTrue(() => {
//         context.getEntityBy(filter).get(number.of(0))
//           .map(v => v.getText(string.of("isModified")).ifPresent(v => {
//             v.set(string.of("Yes"))
//           }))
//       })
//     }
//   })
//  
//   hostApi.runtime.registerEffect({
//     name: "repeat",
//     reoccurAfterMs: (context, executionCount, input, output) => {
//       return context.getEntityBy(filter)
//         .get(number.of(0))
//         .flatMap(elementExpr => elementExpr.getNumber(key))
//         .isCondition(value => value.isLessOrEqualTo(number.of(20)))
//         .getOnTrueOrFalse(hostApi.runtime.maybe.of(hostApi.runtime.number.of(1)), hostApi.runtime.maybe.none());
//     },
//     prepare: (context, input) => {
//       return context.emitEvent(string.of("key-modify-if-par"), {})
//     },
//     apply: (context, output) => {
//       context.getEntityBy(filter)
//         .map(elementExpr => {
//           elementExpr.getNumber(key).map(v => v.sum(number.of(1)));
//         })
//     }
//   })
//
//   hostApi.runtime.emitEvent("repeat", {});
//
//   hostApi.runtime.registerAnimation(hostApi.runtime.string.of("hover"), {
//     frames: [
//       { sprite: hostApi.ui.getSpritePNG("hover.png") },
//     ],
//     duration: hostApi.runtime.number.of(1),
//   });
//   hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
//     frames: [
//       { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
//       { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
//       { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
//       { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
//       { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
//     ],
//     duration: hostApi.runtime.number.of(5),
//     loop: true,
//   });
//   hostApi.runtime.registerAnimation(hostApi.runtime.string.of("textureSlow"), {
//     frames: [
//       { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
//       { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
//       { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
//       { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
//       { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
//     ],
//     duration: hostApi.runtime.number.of(30),
//     loop: true,
//   });
//   hostApi.ui.panel("center", {
//     x: 70,
//     y: 70,
//     width: 100,
//     height: 100,
//     onHover: {
//       texture: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover")),
//       thickness: 5,
//     },
//     background: hostApi.ui.getAnimation(hostApi.runtime.string.of("texture")),
//   }, [
//     hostApi.ui.field("centerContent", { entity: "entity_id", map: "number", name: "key", fallback: "0", align: "top" }),
//   ])
//
//
//   hostApi.ui.panel("isModifiedPanel", {
//     x: 250,
//     y: 100,
//     width: 100,
//     height: 100,
//     onHover: {
//       texture: hostApi.ui.getAnimation(hostApi.runtime.string.of("hover")),
//       thickness: 10,
//     },
//     background: hostApi.ui.getAnimation(hostApi.runtime.string.of("textureSlow")),
//   }, [
//     hostApi.ui.field("isModifiedContent", { entity: "entity_id", map: "text", name: "isModified", fallback: "No", align: "center" }),
//   ])
// }

export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.registerAnimation(hostApi.runtime.string.of("texture"), {
    frames: [
      { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
      { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
    ],
    duration: hostApi.runtime.number.of(5),
    loop: true,
  });
  
  hostApi.runtime.registerEntity({
    id: "node-1",
    numberMap: {
      column: number.of(2),
      row: number.of(1),
    },
  });

  hostApi.runtime.registerContainer({
    id: "grid-1",
    entities: [
      hostApi.runtime.string.of("node-1"),
    ],
    background: {
      frames: [
        { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
        { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
        { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
        { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
        { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
      ],
      duration: hostApi.runtime.number.of(5),
      loop: true,
    },
    getX: (entity) => entity.number_map.get("column").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    getSpanX: (entity) => number.of(1),
    getSpanY: (entity) => number.of(1),
    sizeX: {
      value: number.of(10),
      outOfBounds: "clamp",
    },
    sizeY: {
      value: number.of(5),
      outOfBounds: "clamp",
    },
  });

  // The click handler forwards the cursor cell as the teleport destination.
  hostApi.runtime.registerAction({
    name: string.of("teleport-to-cursor"),
    apply: (ctx) => {
      ctx.teleportTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: ctx.args.x,
        y: ctx.args.y,
        clamp: true,
      });
    },
  });

  hostApi.ui.panel("board", {
    width: 300,
    height: 300,
    layout: {
      columns: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
      rows: [{ scale: 1 }, { scale: 1 }, { scale: 1 }],
    },
    background: {
      frames: [
        { sprite: hostApi.ui.getSpritePNG("frame_1.png") },
        { sprite: hostApi.ui.getSpritePNG("frame_2.png") },
        { sprite: hostApi.ui.getSpritePNG("frame_3.png") },
        { sprite: hostApi.ui.getSpritePNG("frame_4.png") },
        { sprite: hostApi.ui.getSpritePNG("frame_5.png") },
      ],
      duration: hostApi.runtime.number.of(5),
      loop: true,
    },
    onClick: (ctx) => {
      ctx.emitAction("teleport-to-cursor", {
        x: ctx.cursor.getX(),
        y: ctx.cursor.getY(),
      });
    },
  }, [])
}
