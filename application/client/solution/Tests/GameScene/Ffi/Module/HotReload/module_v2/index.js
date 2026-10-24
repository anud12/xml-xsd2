/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string} = hostApi.runtime;

  const node1 = hostApi.runtime.setEntity({
    id: string.of("node-1"),
    numberMap: {
      "key": number.of(0)
    }
  });

  // New in v2: an extra entity...
  const node2 = hostApi.runtime.setEntity({
    id: string.of("node-2"),
    numberMap: {
      "x": number.of(1),
      "y": number.of(1)
    }
  });

  // ...and a container holding both entities.
  hostApi.runtime.setContainer({
    id: "grid-2",
    entities: [
      node1,
      node2,
    ],
    getX: (entity) => entity.number_map.get("x").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("y").orElse(number.of(0)),
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

  // The v2 module reoccurs every 5 units instead of every 10.
  const repeat = hostApi.runtime.setEffect({
    name: string.of("repeat"),
    reoccurAfterMs: (context, executionCount, input, output) => {
      return hostApi.runtime.maybe.of(number.of(5));
    },
    apply: (context, output) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("node-1"))],
      })
        .map(elementExpr => {
          elementExpr.number_map.put("key", (current) => current.map(v => v.sum(number.of(1))));
        })
    }
  });

  // New in v2: an extra action the reloaded module registers.
  hostApi.runtime.setAction({
    name: string.of("ping"),
    apply: () => {
      hostApi.runtime.log("___hot-reload ping fired___");
    }
  });

  hostApi.runtime.emitEvent(repeat, {});

  // Same panel id, new texture in v2. A still image is a single-frame
  // animation.
  const tex2 = hostApi.runtime.setAnimation(string.of("tex2"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("tex2.png") }],
    duration: number.of(1),
  });

  hostApi.ui.window("badge", {
    panel: {
      width: 60,
      height: 60,
      x: 10,
      y: 10,
      background: { animation: tex2, fill: { width: "stretch", height: "stretch", keepAspectRatio: true } },
    },
  }, [
    hostApi.ui.field("content", { entity: node1, map: "number", name: "key", fallback: "0" }),
  ]);
}
