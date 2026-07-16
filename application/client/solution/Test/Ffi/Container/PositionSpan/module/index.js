// @ts-nocheck
export default (hostApi) => {
  const { number } = hostApi.runtime;

  hostApi.runtime.registerEntity({
    id: "sword-1",
    numberMap: {
      slotIndex: number.of(3),
      slotSpan: number.of(2),
    },
  });

  hostApi.runtime.registerEntity({
    id: "potion-1",
    numberMap: {
      slotIndex: number.of(0),
      slotSpan: number.of(1),
    },
  });

  hostApi.runtime.registerEntity({
    id: "shield-1",
    numberMap: {
      slotIndex: number.of(5),
      slotSpan: number.of(1),
    },
  });

  hostApi.runtime.registerContainer({
    id: "bag-1",
    entities: [
      hostApi.runtime.string.of("sword-1"),
      hostApi.runtime.string.of("potion-1"),
      hostApi.runtime.string.of("shield-1"),
    ],
    getX: (entity) => entity.number_map.get("slotIndex").orElse(number.of(0)),
    getY: (entity) => number.of(0),
    getSpanX: (entity) => entity.number_map.get("slotSpan").orElse(number.of(1)),
    getSpanY: (entity) => number.of(1),
  });
}
