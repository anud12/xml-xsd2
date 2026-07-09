// @ts-nocheck
export default (hostApi) => {
  const { string } = hostApi;
  hostApi.registerContainer({
    id: "bag-1",
    entities: {
      entity: [
        string.of("sword-1"),
        string.of("potion-1"),
        string.of("shield-1"),
      ],
    },
  });
}
