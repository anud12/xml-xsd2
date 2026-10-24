/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string, condition } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: "entity-1",
    numberMap: { value_a: number.of(100), value_b: number.of(1) },
    textMap: { label: string.of("initial") }
  });

  const mutateEffect = hostApi.runtime.setEffect({
    name: string.of("mutate"),
    apply: (context) => {
      context.getMutableEntityBy({ filters: [
        (e) => e.id.isContainingExactly(string.of("entity-1"))
      ]}).forEach((entity) => {
        entity.number_map.put("value_a", () => hostApi.runtime.maybe.of(number.of(80)));
        entity.number_map.put("value_b", () => hostApi.runtime.maybe.of(number.of(2)));
        entity.text_map.put("label", () => hostApi.runtime.maybe.of(string.of("updated")));
        hostApi.runtime.log("value_a=" + entity.number_map.get("value_a").orElse(number.of(0)));
        hostApi.runtime.log("value_b=" + entity.number_map.get("value_b").orElse(number.of(0)));
        hostApi.runtime.log("label=" + entity.text_map.get("label").orElse(string.of("")));
        entity.number_map.put("value_b", () => hostApi.runtime.maybe.none());
        let present = false;
        entity.number_map.get("value_b").isPresent().ifTrue(() => { present = true; });
        hostApi.runtime.log("value_b=" + (present ? "present" : "absent"));
      });
    }
  });

  hostApi.runtime.setAction({
    name: string.of("mutate"),
    apply: (context) => {
      context.emitEffect(mutateEffect, {});
    }
  });
}
