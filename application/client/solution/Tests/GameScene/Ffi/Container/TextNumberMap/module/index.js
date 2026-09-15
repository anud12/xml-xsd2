export default (hostApi) => {
  const { number } = hostApi.runtime;
  hostApi.runtime.registerContainer({
    id: "bag-1",
    textMap: {
      label: "Main Bag",
    },
    numberMap: {
      capacity: number.of(20),
    },
  });
}
