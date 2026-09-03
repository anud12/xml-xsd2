export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.registerEntity({
    id: "node-1",
    numberMap: {
      column: number.of(2),
      row: number.of(1),
    },
  });

  hostApi.runtime.registerEntity({
    id: "node-2",
    numberMap: {
      column: number.of(0),
      row: number.of(0),
    },
  });

  hostApi.runtime.registerContainer({
    id: "grid-1",
    entities: [
      hostApi.runtime.string.of("node-1"),
      hostApi.runtime.string.of("node-2"),
    ],
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

  hostApi.runtime.registerAction({
    name: string.of("relocate-node-1"),
    apply: (ctx) => {
      ctx.teleportTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 6,
        y: 3,
        clamp: true,
      });
    },
  });

  hostApi.runtime.registerAction({
    name: string.of("relocate-node-1-out-of-bounds"),
    apply: (ctx) => {
      ctx.teleportTo({
        containerId: string.of("grid-1"),
        entityId: string.of("node-1"),
        x: 15,
        y: 9,
        clamp: true,
      });
    },
  });
}
