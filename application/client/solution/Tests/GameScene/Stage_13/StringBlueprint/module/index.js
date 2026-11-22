/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { string, stringBlueprint: sg } = hostApi.runtime;

  // A "person name" grammar: <first> + " " + <last>. Each part draws from two
  // alternatives, so the composed language is their ordered product.
  const first = sg.oneOf(() => [string.of("Al"), string.of("Bo")]);
  const last = sg.oneOf(() => [string.of("Smith"), string.of("Jones")]);
  const name = sg.concat([
    first.generateValue(),
    string.of(" "),
    last.generateValue(),
  ]);

  // Full-match membership probe -> "1"/"0". ConditionExpression has no boolean
  // accessor in the type contract, so flip a flag via ifTrue (runs when true).
  const probe = (candidate) => {
    let v = "0";
    name.isGeneratedBy(candidate).ifTrue(() => {
      v = "1";
      return string.of("1");
    });
    return v;
  };

  const present = name.generateValue() ? "1" : "0";
  let emptyAbsent = "0";
  // @ts-expect-error Exercise the runtime check for untyped callers.
  try { sg.oneOf(() => []).generateValue(); } catch (_) { emptyAbsent = "1"; }

  // Self-consistency: a generated grammar is a member of its own language.
  const sample = name.generateValue();

  // Membership probe for any grammar + candidate -> "1"/"0".
  const isMember = (grammar, candidate) => {
    let v = "0";
    grammar.isGeneratedBy(candidate).ifTrue(() => {
      v = "1";
      return string.of("1");
    });
    return v;
  };

  // Concrete generateValue() results are RANDOM members of the language, drawn
  // from the shared seeded LXM stream. So the exact string is not fixed; each
  // is verified as a valid member of its own language (isGeneratedBy). The raw
  // values are also exposed for the seed-reproducibility test. Absent (empty
  // language) -> the orElse fallback.
  const genName = name.generateValue();
  const genFirst = first.generateValue();
  const genConcatGen = sg.concat([
    sg.oneOf(() => [string.of("x"), string.of("y")]).generateValue(),
    sg.oneOf(() => [string.of("1"), string.of("2")]).generateValue(),
  ]);
  const genConcat = genConcatGen.generateValue();
  const genFallback = string.of("FALLBACK");

  hostApi.runtime.setEntity({
    id: string.of("sg"),
    stringMap: { alSmith: string.of(probe(string.of("Al Smith"))), alJones: string.of(probe(string.of("Al Jones"))), boSmith: string.of(probe(string.of("Bo Smith"))), boJones: string.of(probe(string.of("Bo Jones"))), substring: string.of(probe(string.of("Smith"))), prefix: string.of(probe(string.of("Bo Jon"))), notInVocab: string.of(probe(string.of("Zed Jones"))), caseSensitive: string.of(probe(string.of("al smith"))), present: string.of(present), emptyAbsent: string.of(emptyAbsent), sampleIsMember: string.of(probe(sample)), genName: genName, genFirst: genFirst, genConcat: genConcat, genFallback: genFallback, genNameIsMember: string.of(isMember(name, genName)), genFirstIsMember: string.of(isMember(first, genFirst)), genConcatIsMember: string.of(isMember(genConcatGen, genConcat)) },
  });
};
