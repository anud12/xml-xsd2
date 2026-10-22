/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const { string, stringGenerator: sg } = hostApi.runtime;

  // A "person name" grammar: <first> + " " + <last>. Each part draws from two
  // alternatives, so the composed language is their ordered product.
  const first = sg.oneOf([string.of("Al"), string.of("Bo")]);
  const last = sg.oneOf([string.of("Smith"), string.of("Jones")]);
  const name = sg.concat([
    first.generateValue().orElse(string.of("")),
    string.of(" "),
    last.generateValue().orElse(string.of("")),
  ]);

  // Full-match membership probe -> "1"/"0". ConditionExpression has no boolean
  // accessor in the type contract, so flip a flag via ifTrue (runs when true).
  const probe = (candidate) => {
    let v = "0";
    name.isGeneratedBy(candidate).ifTrue(() => {
      v = "1";
    });
    return v;
  };

  // generateValue() presence for a non-empty language; absence for oneOf([]).
  let present = "0";
  name.generateValue().isPresent().ifTrue(() => {
    present = "1";
  });
  let emptyAbsent = "0";
  sg.oneOf([]).generateValue().isEmpty().ifTrue(() => {
    emptyAbsent = "1";
  });

  // Self-consistency: a generated grammar is a member of its own language.
  const sample = name.generateValue().orElse(string.of(""));

  // Concrete generateValue() results. In this runtime the generated value is
  // the deterministic representative member of the language (the first one),
  // so the exact string is known. Absent (empty language) -> the orElse fallback.
  const genName = name.generateValue().orElse(string.of("FALLBACK")); // "Al Smith"
  const genNameRepeat = name.generateValue().orElse(string.of("FALLBACK")); // deterministic
  const genFirst = first.generateValue().orElse(string.of("NOPE")); // "Al"
  const genConcat = sg.concat([
    sg.oneOf([string.of("x"), string.of("y")]).generateValue().orElse(string.of("")),
    sg.oneOf([string.of("1"), string.of("2")]).generateValue().orElse(string.of("")),
  ]).generateValue().orElse(string.of("NOPE")); // "x1"
  const genFallback = sg.oneOf([]).generateValue().orElse(string.of("FALLBACK")); // "FALLBACK"

  hostApi.runtime.setEntity({
    id: string.of("sg"),
    textMap: {
      // every (first x last) product member is generated
      alSmith: string.of(probe(string.of("Al Smith"))),
      alJones: string.of(probe(string.of("Al Jones"))),
      boSmith: string.of(probe(string.of("Bo Smith"))),
      boJones: string.of(probe(string.of("Bo Jones"))),
      // full-match: NOT substring / prefix / different case / out-of-vocab
      substring: string.of(probe(string.of("Smith"))),
      prefix: string.of(probe(string.of("Bo Jon"))),
      notInVocab: string.of(probe(string.of("Zed Jones"))),
      caseSensitive: string.of(probe(string.of("al smith"))),
      // generateValue present for non-empty language, absent for the empty one
      present: string.of(present),
      emptyAbsent: string.of(emptyAbsent),
      // a grammar value is accepted by its own isGeneratedBy
      sampleIsMember: string.of(probe(sample)),
      // generateValue concrete results (deterministic representative member):
      // a composed product yields first-x-first, oneOf yields its first
      // alternative, concat composes its parts, and an empty language yields
      // the orElse fallback.
      genName: genName,
      genNameRepeat: genNameRepeat,
      genFirst: genFirst,
      genConcat: genConcat,
      genFallback: genFallback,
    },
  });
};
