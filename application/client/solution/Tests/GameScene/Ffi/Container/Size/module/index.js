export default (hostApi) => {
  const { number } = hostApi.runtime;

  hostApi.runtime.setContainer({
    id: "bag-1",
    sizeX: {
      value: number.of(20),
      outOfBounds: "clamp",
    },
    sizeY: {
      value: number.of(1),
      outOfBounds: "clamp",
    },
  });

  hostApi.runtime.setContainer({
    id: "chest-grid-1",
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
