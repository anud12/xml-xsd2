export default (hostApi) => {
  const { number } = hostApi.runtime;
  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("bag-1"),
    entityListMap: { members: [] },
    numberMap: { capacity: number.of(20) },
    stringMap: { label: "Main Bag" },
  });
}
