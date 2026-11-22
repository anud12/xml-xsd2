export default (hostApi) => {
  const { number } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("bag-1"),
    entityListMap: { members: [] },
    numberMap: { sizeX: number.of(20), sizeY: number.of(1) },
    stringMap: { outOfBoundsX: "clamp", outOfBoundsY: "clamp" },
  });

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("chest-grid-1"),
    entityListMap: { members: [] },
    numberMap: { sizeX: number.of(6), sizeY: number.of(4) },
    stringMap: { outOfBoundsX: "wrap", outOfBoundsY: "wrap" },
  });
}
