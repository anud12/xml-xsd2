/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string} = hostApi.runtime;

  hostApi.runtime.setEntity({
    id: string.of("m"),
    textMap: { flag: string.of("no") }
  });

  // Two keys at once: malformed.
  const twoKeys = {
    filters: [(e) => e.id.isContainingExactly(string.of("m"))],
    and: [{ filters: [] }],
  };
  const mark1 = hostApi.runtime.setEffect({
    name: string.of("mark-two-keys"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      ctx.getMutableEntityBy(twoKeys)
        .map(ee => ee.text_map.get(string.of("flag")).ifPresent(v => {
          v.map(string.of("marked"));
        }))
    }
  });

  // No known key at all: malformed.
  const mark2 = hostApi.runtime.setEffect({
    name: string.of("mark-unknown-key"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      /** @type {any} */
      const unknown = { something: string.of("else") };
      ctx.getMutableEntityBy(unknown)
        .map(ee => ee.text_map.get(string.of("flag")).ifPresent(v => {
          v.map(string.of("marked"));
        }))
    }
  });

  // A plain string is no longer an accepted input: malformed.
  const mark4 = hostApi.runtime.setEffect({
    name: string.of("mark-plain-string"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      /** @type {any} */
      const asString = string.of("m");
      ctx.getMutableEntityBy(asString)
        .map(ee => ee.text_map.get(string.of("flag")).ifPresent(v => {
          v.map(string.of("marked"));
        }))
    }
  });

  // The legacy { id: fn } shape is no longer accepted: malformed.
  const mark5 = hostApi.runtime.setEffect({
    name: string.of("mark-legacy-id"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      /** @type {any} */
      const legacy = { id: id => id.isContainingExactly(string.of("m")) };
      ctx.getMutableEntityBy(legacy)
        .map(ee => ee.text_map.get(string.of("flag")).ifPresent(v => {
          v.map(string.of("marked"));
        }))
    }
  });

  // null: malformed.
  const mark3 = hostApi.runtime.setEffect({
    name: string.of("mark-null"),
    prepare: () => ({value: true}),
    apply: (ctx) => {
      // @ts-expect-error deliberately malformed filter for error-path test
      ctx.getMutableEntityBy(null)
        .map(ee => ee.text_map.get(string.of("flag")).ifPresent(v => {
          v.map(string.of("marked"));
        }))
    }
  });

  hostApi.runtime.emitEvent(mark1, {});
  hostApi.runtime.emitEvent(mark2, {});
  hostApi.runtime.emitEvent(mark4, {});
  hostApi.runtime.emitEvent(mark5, {});
  hostApi.runtime.emitEvent(mark3, {});
};
