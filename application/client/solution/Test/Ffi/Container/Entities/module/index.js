// @ts-nocheck
export default (hostApi) => {
  hostApi.registerContainer({
    id: "bag-1",
    entities: [
      { entityIdReference: "sword-1" },
      { entityIdReference: "potion-1" },
      { entityIdReference: "shield-1" },
    ],
  });
}
