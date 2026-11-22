export default (hostApi) => {
  const { string, number, stringBlueprint: sg, entityBlueprint: eg } = hostApi.runtime;

  // Read a raw value out of a runtime Maybe (an entity-proxy field).
  const raw = (maybe) => {
    let v = null;
    if (maybe && typeof maybe.ifPresent === "function") {
      maybe.ifPresent((w) => { v = (w && w._raw !== undefined) ? w._raw : (w ? w.value : null); });
    }
    return v;
  };

  // L(gen) = { alpha/10, beta/10, static/20 }. The blueprint set is produced by
  // the list-maker lambda (evaluated at generateValue()/isGeneratedBy() time); a
  // generated "name" grammar (two alternatives) feeds blueprint A. Blueprints
  // carry no id; generateValue() assigns one at instantiation.
  const gen = eg.oneOf(() => {
    const names = sg.oneOf(() => [string.of("alpha"), string.of("beta")]);
    const a = {
      textMap: { name: names.generateValue() },
      numberMap: { hp: number.of(10) },
    };
    const b = {
      textMap: { name: string.of("static") },
      numberMap: { hp: number.of(20) },
    };
    return [a, b];
  });

  // generateValue() instantiates an entity and returns a read-only expression.
  const present = gen.generateValue() ? "1" : "0";
  let emptyAbsent = "0";
  try { eg.oneOf(() => []).generateValue(); } catch (_) { emptyAbsent = "1"; }

  // A concrete sample: instantiated as a new entity, returned as a proxy.
  const sample = gen.generateValue();
  const sampleId = String(sample.id);
  const name = (sample && sample.string_map && typeof sample.string_map.get === "function")
    ? (raw(sample.string_map.get("name")) || "none") : "none";
  const hpVal = (sample && sample.number_map && typeof sample.number_map.get === "function")
    ? raw(sample.number_map.get("hp")) : null;
  const hp = (hpVal === null) ? -1 : Number(hpVal);

  // The generated entity (read back through its proxy) is a language member.
  let sampleIsMember = "0";
  gen.isGeneratedBy(sample).ifTrue(() => { sampleIsMember = "1"; return string.of("1"); });

  // Exact-field membership probes over the language (name + hp pairs).
  const memberProbe = (n, hp) => {
    let v = "0";
    gen.isGeneratedBy({ textMap: { name: string.of(n) }, numberMap: { hp: number.of(hp) } })
      .ifTrue(() => { v = "1"; return string.of("1"); });
    return v;
  };

  hostApi.runtime.setEntity({
    id: string.of("probe"),
    numberMap: { genHp: number.of(hp) },
    stringMap: { present: string.of(present), emptyAbsent: string.of(emptyAbsent), sampleIsMember: string.of(sampleIsMember), sampleId: string.of(sampleId), genName: string.of(String(name)), alpha10: string.of(memberProbe("alpha", 10)), beta10: string.of(memberProbe("beta", 10)), static10: string.of(memberProbe("static", 10)), static20: string.of(memberProbe("static", 20)), alpha20: string.of(memberProbe("alpha", 20)), static99: string.of(memberProbe("static", 99)) },
  });
};
