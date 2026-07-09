// @ts-nocheck
export default (hostApi) => {
  const { number } = hostApi;
  hostApi.registerContainer({
    id: "bag-1",
    textMap: {
      label: "Main Bag",
    },
    numberMap: {
      capacity: number.of(20),
    },
  });
}
