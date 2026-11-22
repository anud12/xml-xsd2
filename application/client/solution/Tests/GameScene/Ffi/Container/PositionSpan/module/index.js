export default (hostApi) => {
  const { number } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("sword-1"),
    numberMap: { slotIndex: number.of(3), spanX: number.of(2) },
    stringMap: { xKey: "slotIndex" },
  });

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("potion-1"),
    numberMap: { slotIndex: number.of(0), spanX: number.of(1) },
    stringMap: { xKey: "slotIndex" },
  });

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("shield-1"),
    numberMap: { slotIndex: number.of(5), spanX: number.of(1) },
    stringMap: { xKey: "slotIndex" },
  });

  hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("bag-1"),
    entityListMap: { members: [
      hostApi.runtime.string.of("sword-1"),
      hostApi.runtime.string.of("potion-1"),
      hostApi.runtime.string.of("shield-1"),
    ] },
  });
}
