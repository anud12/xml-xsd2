export default (hostApi) => {
  const { string } = hostApi.runtime;
  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("bag-1"),
    entityListMap: { members: [
      string.of("sword-1"),
      string.of("potion-1"),
      string.of("shield-1"),
    ] },
  });
}
