/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { number, string, condition } = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("entity-1"),
    numberMap: { value_a: number.of(100), value_b: number.of(1) },
    stringMap: { label: string.of("initial"), filter_key: string.of("entity-1") },
  });

  const mutateEffect = hostApi.runtime.setEffect({
    name: string.of("mutate"),
    apply: (context) => {
      context.getMutableEntityBy({ filters: [
        (e) => e.string_map.get("filter_key")
          .map(t => t.isContainingExactly(string.of("entity-1")))
          .orElse(condition.of(false))
      ]}).exactlyOne().ifPresent((entity) => {
        entity.number_map.put("value_a", () => hostApi.runtime.maybe.of(number.of(80)));
        entity.number_map.put("value_b", () => hostApi.runtime.maybe.of(number.of(2)));
        entity.string_map.put("label", () => hostApi.runtime.maybe.of(string.of("updated")));
        hostApi.runtime.log("value_a=" + entity.number_map.get("value_a").orElse(number.of(0)));
        hostApi.runtime.log("value_b=" + entity.number_map.get("value_b").orElse(number.of(0)));
        hostApi.runtime.log("label=" + entity.string_map.get("label").orElse(string.of("")));
        entity.number_map.put("value_b", () => hostApi.runtime.maybe.none());
        let present = false;
        entity.number_map.get("value_b").isPresent().ifTrue(() => { present = true; return number.of(1); });
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
