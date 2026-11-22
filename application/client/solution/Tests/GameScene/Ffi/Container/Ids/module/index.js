export default (hostApi) => {
  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("bag-1"),
    entityListMap: { members: [] },
  });
  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("chest-grid-1"),
    entityListMap: { members: [] },
  });
}
