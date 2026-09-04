export default (hostApi) => {
  const { number, string } = hostApi.runtime;

  hostApi.runtime.registerEntity({
    id: "actor-1",
    numberMap: { column: number.of(0), row: number.of(0) },
  });
  hostApi.runtime.registerEntity({
    id: "actor-2",
    numberMap: { column: number.of(0), row: number.of(0) },
  });

  // Parks an interruptible plan: while parked the actor is busy and its active
  // action is this one; a new action may replace it (interrupt).
  hostApi.runtime.registerAction({
    name: string.of("long-action"),
    apply: (ctx) => {
      ctx.allowInterrupt();
      ctx.wait(10);
    },
  });

  // A second parking action, so a replacement can change the active action to a
  // different name.
  hostApi.runtime.registerAction({
    name: string.of("other-action"),
    apply: (ctx) => {
      ctx.allowInterrupt();
      ctx.wait(10);
    },
  });

  // Runs without parking: it discards the actor's parked plan, so there is no
  // active action afterwards.
  hostApi.runtime.registerAction({
    name: string.of("instant-action"),
    apply: (ctx) => {
      ctx.allowInterrupt();
    },
  });
}
