export default (hostApi) => {
  const { string } = hostApi.runtime;
  hostApi.runtime.registerContainer({
    id: "bag-1",
    entities: [
      string.of("sword-1"),
      string.of("potion-1"),
      string.of("shield-1"),
    ],
  });
}
