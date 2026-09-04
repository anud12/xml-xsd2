export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.registerEntity({
    id: "worker-1",
    numberMap: {
      column: number.of(0),
      row: number.of(0),
    },
  });

  // Each operation is an effect that logs a distinct line, so the test can
  // tell which operations actually ran and which were dropped.
  hostApi.runtime.registerEffect({
    name: "task-start",
    apply: () => { hostApi.runtime.log("___interrupt task start fired___"); },
  });
  hostApi.runtime.registerEffect({
    name: "task-step-one",
    apply: () => { hostApi.runtime.log("___interrupt task step one fired___"); },
  });
  hostApi.runtime.registerEffect({
    name: "task-step-two",
    apply: () => { hostApi.runtime.log("___interrupt task step two fired___"); },
  });
  hostApi.runtime.registerEffect({
    name: "instant-step",
    apply: () => { hostApi.runtime.log("___interrupt instant fired___"); },
  });

  // Main action: one operation before it allows interruption, then two more
  // after a wait with a wait between them. While parked it stays interruptible,
  // so a repeated submit drops the parked plan instead of queueing behind it.
  hostApi.runtime.registerAction({
    name: string.of("begin-task"),
    apply: (ctx) => {
      ctx.emitEffect("task-start", {});
      ctx.allowInterrupt();
      ctx.wait(10);
      ctx.emitEffect("task-step-one", {});
      ctx.denyInterrupt();
      ctx.wait(10);
      ctx.emitEffect("task-step-two", {});
    },
  });

  // Instant action: a single operation, no wait. It replaces any parked plan.
  hostApi.runtime.registerAction({
    name: string.of("instant-task"),
    apply: (ctx) => {
      ctx.emitEffect("instant-step", {});
    },
  });
}
