export default (hostApi) => {
  const { string, number, numberBlueprint: nb, entityBlueprint: eg } = hostApi.runtime;

  // Read a raw value out of a runtime Maybe (an entity-proxy field).
  const raw = (maybe) => {
    let v = null;
    if (maybe && typeof maybe.ifPresent === "function") {
      maybe.ifPresent((w) => { v = (w && w._raw !== undefined) ? w._raw : (w ? w.value : null); });
    }
    return v;
  };

  // A range blueprint: the closed integer interval [1, 10].
  const r = nb.range(1, 10);

  // generateValue() on a non-empty range is present, and yields a value in range.
  let present = "0";
  r.generateValue().isPresent().ifTrue(() => { present = "1"; return string.of("1"); });
  const rv = r.generateValue().orElse(number.of(-1));
  const rVal = (rv && rv.value !== undefined) ? Number(rv.value) : -1;
  const inRange = (rVal >= 1 && rVal <= 10) ? "1" : "0";

  // A two-piece union: [1,3] and [5,7]; 4 is a gap between the pieces.
  const two = nb.oneOf(() => [nb.range(1, 3), nb.range(5, 7)]);
  const twoPieces = String(two.pieces().length);
  const gap = two.isGeneratedBy(4).value ? "1" : "0";
  const inP1 = two.isGeneratedBy(2).value ? "1" : "0";
  const inP2 = two.isGeneratedBy(6).value ? "1" : "0";

  // A number blueprint plugged into an entity's numberMap field: "code" is
  // generated from the [1,10] range, "kind" is the fixed literal 0.
  const gen = eg.oneOf(() => [
    { textMap: { tag: string.of("n") }, numberMap: { code: r, kind: number.of(0) } },
  ]);
  const fallback = { textMap: { tag: string.of("n") }, numberMap: { code: number.of(-1), kind: number.of(-1) } };
  const sample = gen.generateValue().orElse(fallback);
  const code = (sample && sample.number_map && typeof sample.number_map.get === "function")
    ? Number(raw(sample.number_map.get("code"))) : -999;
  const kind = (sample && sample.number_map && typeof sample.number_map.get === "function")
    ? Number(raw(sample.number_map.get("kind"))) : -999;
  const codeInRange = (code >= 1 && code <= 10) ? "1" : "0";
  const kindZero = (kind === 0) ? "1" : "0";
  let sampleMember = "0";
  gen.isGeneratedBy(sample).ifTrue(() => { sampleMember = "1"; return string.of("1"); });

  hostApi.runtime.setEntity({
    id: string.of("probe"),
    textMap: {
      present: string.of(present),
      inRange: string.of(inRange),
      twoPieces: string.of(twoPieces),
      gap: string.of(gap),
      inP1: string.of(inP1),
      inP2: string.of(inP2),
      codeInRange: string.of(codeInRange),
      kindZero: string.of(kindZero),
      sampleMember: string.of(sampleMember),
    },
    numberMap: { rangeVal: number.of(rVal), code: number.of(code) },
  });
};
