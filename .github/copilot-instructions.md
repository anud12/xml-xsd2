# Copilot instructions — xml-xsd2 workspace

Purpose
-------
Practical guidance for future Copilot CLI sessions working in this workspace and the related implementation repository. Focuses on concrete commands, where to find behavior (tests/specs), and repo-specific conventions that Copilot should respect when making edits.

## Project purpose
- This workspace contains specification documents (this repo: xml-xsd2) that describe the data model (in an data format agnostic way) and expected behavior of the `sibling` repository at "../xml-xsd/implementation".
- The test suite (`specification`) for `sibling` is stored at "../xml-xsd/specification"


Key runtime concepts are implemented there:
  - WorldStep model: XML-derived model types (WorldStep, RuleGroup, NameRule, Entry, etc.)
  - Model classes are generated / structured to mirror XML schema (packages reflect element paths).
  - Runtime instance: WorldStepInstance boots and indexes all rule repositories (property, name, zone, region, entity, container, ...).
  - Repositories: index() builds id->Entry maps used for fast lookups (e.g., name rules repository).
  - Resolution logic: Calculate* classes (CalculateName) implement evaluation semantics (concatenation, refs, one_of/choice selection).
  - Deterministic randomness: WorldStepInstance.random()/randomFrom() uses WorldMetadata.RandomizationTable + internal counter for reproducible selection.
  - WebSocket & test harness: specification tests use a websocket-based analyzer (HttpTestBase / analyzeExecuteNameRule) to validate behavior against the implementation.

###

## High-level architecture (big picture)

- This workspace contains specification documents (this repo: xml-xsd2) that describe the data model (in an data format agnostic way) and expected behavior.
- The Java implementation lives in a sibling repository at ../xml-xsd/implementation. Key runtime concepts are implemented there:
  - WorldStep model: XML-derived model types (WorldStep, RuleGroup, NameRule, Entry, etc.)
  - Model classes are generated / structured to mirror XML schema (packages reflect element paths).
  - Runtime instance: WorldStepInstance boots and indexes all rule repositories (property, name, zone, region, entity, container, ...).
  - Repositories: index() builds id->Entry maps used for fast lookups (e.g., name rules repository).
  - Resolution logic: Calculate* classes (CalculateName) implement evaluation semantics (concatenation, refs, one_of/choice selection).
  - Deterministic randomness: WorldStepInstance.random()/randomFrom() uses WorldMetadata.RandomizationTable + internal counter for reproducible selection.
  - WebSocket & test harness: specification tests use a websocket-based analyzer (HttpTestBase / analyzeExecuteNameRule) to validate behavior against the implementation.

## Key conventions and patterns (repo-specific)

- XML-to-Java mapping:
  - Generated/hand-crafted model classes mirror XML paths: e.g., ro.anud.xml_xsd.implementation.model.Type_nameToken.NameToken.NameToken
  - Each model class exposes:
    - static nodeName (XML element name)
    - fromRawNode / deserialize / serializeIntoRawNode methods for RawNode-based parsing and serialization
    - builder pattern and Lombok annotations
    - parent/child linking via LinkedNode and change/remove subscriptions (onChange/onRemove)

- RawNode/LinkedNode pattern:
  - XML is parsed into RawNode objects; model classes convert RawNode -> typed model via deserialize.
  - LinkedNode provides parent/child links and change notifications. Keep these invariants when editing model/serialization code.

- Indexing pattern:
  - Many runtime services expose index() which populates in-memory repositories used at runtime. After changing model shapes, ensure index() logic still finds stream* methods (e.g., streamRuleGroup, streamEntry).

- Logging scope:
  - Code uses LogScope (try-with-resources) for structured, contextual logging. Preserve logScope usage for consistency.

- Randomness & determinism:
  - random()/randomFrom() are intentionally deterministic for replayable tests. Avoid introducing non-deterministic RNGs without providing seeding or a reproducible alternative.

- Naming conventions to respect:
  - Element-based names (type__*, nodeName) and nested packages are used widely; renaming elements requires updating nodeName constants and (de)serialization logic.

- Tests & dynamic test pattern:
  - Integration/spec tests use JUnit DynamicTest factories and helper HttpTestBase.runTestRelativeToClass to execute analyzers. When adding tests, prefer this pattern for websocket-based scenarios.

## Where to look (important files / dirs)

- Spec/docs (this repo):
  - specification.md, entities.md, name.md, container.md, effects.md, actions.md — these describe behavior that implementation must preserve.
- Java implementation (`sibling` repo):
  - ../xml-xsd/implementation/src/main/java — model, service, repository, middleware packages
  - ../xml-xsd/implementation/src/test/java — unit tests for implementation
  - ../xml-xsd/specification-test/src/test/java — specification-driven tests used to validate behavior via websockets

## Quick guidance for Copilot sessions (practical rules)

- When suggesting API or XML element renames, do not keep backward compatibility in mind: the documentation is of a greenfield project using the `sibling` repository.