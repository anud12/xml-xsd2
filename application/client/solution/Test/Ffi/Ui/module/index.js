/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  hostApi.runtime.setEntity(string.of("hero"), {
    numberMap: { hp: number.of(7) }
  });
  hostApi.ui.window("slab-win", { width: 200, height: 100 }, [
    hostApi.ui.text("slab-text", "slab"),
    hostApi.ui.field("slab-field", {
      entity: "hero",
      map: "number",
      name: "hp",
      fallback: "n/a"
    })
  ]);
};
