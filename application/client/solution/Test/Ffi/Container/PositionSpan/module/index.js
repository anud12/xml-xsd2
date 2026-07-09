// @ts-nocheck
export default (hostApi) => {
  const { number } = hostApi;

  hostApi.registerEntity({
    id: "sword-1",
    numberMap: {
      slotIndex: number.of(3),
      slotSpan: number.of(2),
    },
  });

  hostApi.registerEntity({
    id: "potion-1",
    numberMap: {
      slotIndex: number.of(0),
      slotSpan: number.of(1),
    },
  });

  hostApi.registerEntity({
    id: "shield-1",
    numberMap: {
      slotIndex: number.of(5),
      slotSpan: number.of(1),
    },
  });

  hostApi.registerContainer({
    id: "bag-1",
    entities: [
      hostApi.string.of("sword-1"),
      hostApi.string.of("potion-1"),
      hostApi.string.of("shield-1"),
    ],
    getX: (entity) => entity.number_map.get("slotIndex").orElse(number.of(0)),
    getY: (entity) => number.of(0),
    getSpanX: (entity) => entity.number_map.get("slotSpan").orElse(number.of(1)),
    getSpanY: (entity) => number.of(1),
  });
}
