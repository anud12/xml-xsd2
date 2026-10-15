/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string } = hostApi.runtime;
  const hero = hostApi.runtime.setEntity({
    id: string.of("hero"),
    numberMap: { hp: number.of(7), max: number.of(10) }
  });
  hostApi.ui.window("slab-win", { panel: { width: 200, height: 100 } }, [
    hostApi.ui.text("slab-text", "slab"),
    hostApi.ui.field("slab-field", {
      entity: hero,
      map: "number",
      name: "hp",
      fallback: "n/a"
    })
  ]);
  hostApi.ui.progress("slab-progress", {
    panel: {},
    progress: {
      fill: "art/fill.png",
      track: "art/track.png",
      orientation: "horizontal",
      entity: hero,
      value: (entity) => entity.number_map.get("hp").orElse(number.of(0)),
      max: (entity) => entity.number_map.get("max").orElse(number.of(1))
    }
  });
};
