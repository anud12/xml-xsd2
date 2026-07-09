// @ts-nocheck
export default (hostApi) => {
  const { number } = hostApi;

  hostApi.registerEntity({
    id: "sword-1",
    textMap: {
      name: "Iron Sword",
    },
    numberMap: {
      slotIndex: number.of(3),
      slotSpan: number.of(2),
      attack: number.of(15),
    },
  });

  hostApi.registerEntity({
    id: "potion-1",
    textMap: {
      name: "Health Potion",
    },
    numberMap: {
      slotIndex: number.of(0),
      hp_restored: number.of(20),
    },
  });

  hostApi.registerEntity({
    id: "shield-1",
    textMap: {
      name: "Wood Shield",
    },
    numberMap: {
      slotIndex: number.of(5),
      slotSpan: number.of(1),
      defense: number.of(8),
    },
  });

  hostApi.registerContainer({
    id: "bag-1",
    textMap: {
      label: "Main Bag",
    },
    numberMap: {
      capacity: number.of(20),
    },
    entities: [
      { entityIdReference: "sword-1" },
      { entityIdReference: "potion-1" },
      { entityIdReference: "shield-1" },
    ],
    getX: (entity) => entity.number_map.get("slotIndex").orElse(number.of(0)),
    getY: (entity) => number.of(0),
    getSpanX: (entity) => entity.number_map.get("slotSpan").orElse(number.of(1)),
    getSpanY: (entity) => number.of(1),
    sizeX: {
      value: number.of(20),
      outOfBounds: "clamp",
    },
    sizeY: {
      value: number.of(1),
      outOfBounds: "clamp",
    },
  });

  hostApi.registerContainer({
    id: "chest-grid-1",
    textMap: {
      label: "Storage Chest",
    },
    entities: [
      { entityIdReference: "potion-1" },
    ],
    getX: (entity) => entity.number_map.get("row").orElse(number.of(0)),
    getY: (entity) => entity.number_map.get("col").orElse(number.of(0)),
    getSpanX: (entity) => entity.number_map.get("rowSpan").orElse(number.of(1)),
    getSpanY: (entity) => entity.number_map.get("colSpan").orElse(number.of(1)),
    sizeX: {
      value: number.of(6),
      outOfBounds: "wrap",
    },
    sizeY: {
      value: number.of(4),
      outOfBounds: "wrap",
    },
  });
}
