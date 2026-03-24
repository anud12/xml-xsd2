# Randomness — Concepts

This document describes the [deterministic](https://en.wikipedia.org/wiki/Deterministic_algorithm), positional, and table-less randomness system used for runtime-client synchronicity.

## Summary

All randomness in the engine is **deterministic**, **lock-free**, and **stateless**. Instead of drawing from a shared global state or a pre-generated table, every random draw is calculated directly from its "Execution Context." 

This approach provides **infinite resolution** (64-bit), **mathematical fairness** (no [quantization](https://en.wikipedia.org/wiki/Quantization_(signal_processing)) bias), and **maximum performance** ([CPU](https://en.wikipedia.org/wiki/Central_processing_unit)-bound math vs. memory-bound lookups).

---

## Positional Pseudo-Random Number Generator (Calculation-Based)

The engine uses a high-quality, 64-bit hash-based [Pseudo-Random Number Generator](https://en.wikipedia.org/wiki/Pseudorandom_number_generator) (specifically **[SplitMix64](https://rosettacode.org/wiki/Pseudo-random_numbers/Splitmix64)**) to derive a unique random `long` for every point in the game's execution space.

### Features:
1.  **Resolution**: 64-bit precision (no "stepping" or patterns).
2.  **Memory**: 0 Kilobytes memory footprint (saves CPU [cache](https://en.wikipedia.org/wiki/CPU_cache) for entity data).
3.  **Performance**: Pure arithmetic is faster than memory lookups on modern hardware.
4.  **Fairness**: Every possible `long` has an exactly equal chance of being generated.

---

## Execution Context

An `ExecutionContext` is created at every "Entrypoint" (Action, Event, or Entity Update) and passed through the entire execution chain.

### Context Components:
1.  **World Seed**: The global [entropy](https://en.wikipedia.org/wiki/Entropy_(computing)) for the session.
2.  **Tick Identifier**: The current 30 frames per second frame number.
3.  **Source Identifier**: The Identifier of the Entity (Player/Non-Player Character) performing the action.
4.  **Action Identifier**: The Identifier of the Action or Effect being executed.
5.  **Call Index**: A local counter (starts at 0) that increments with every `random()` call within this specific context.

---

## Synchronicity Contract

-   **Parallel Safety**: Multiple threads can process different Actions simultaneously because their `ExecutionContexts` are independent. No global locks or shared state.
-   **Runtime-Client Parity**: As long as the Client knows the initial Entrypoint conditions (Tick, Entity, Action Identifier), it will arrive at the exact same random results as the Runtime.
-   **No Patterns**: The sequence never "wraps" because there is no fixed-length table. Each draw is unique to its coordinates in (Tick, Entity, Action, Call) space.
