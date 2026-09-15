/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {string, temporal} = hostApi.runtime;

  hostApi.runtime.registerEffect({
    name: "restStart",
    apply: () => {
      hostApi.runtime.log("___busy-test rest start fired___");
    }
  });

  hostApi.runtime.registerEffect({
    name: "restEnd",
    apply: () => {
      hostApi.runtime.log("___busy-test rest end fired___");
    }
  });

  hostApi.runtime.registerEffect({
    name: "dashTick",
    apply: () => {
      hostApi.runtime.log("___busy-test dash fired___");
    }
  });

  // Spanning action: emit, wait 2 GTU, emit. Parks between the emits.
  hostApi.runtime.registerAction({
    name: string.of("rest"),
    apply: (ctx) => {
      ctx.emitEffect("restStart", {});
      ctx.wait(temporal.ofTicks(2));
      ctx.emitEffect("restEnd", {});
    }
  });

  // Instant action for the same actor.
  hostApi.runtime.registerAction({
    name: string.of("dash"),
    apply: (ctx) => {
      ctx.emitEffect("dashTick", {});
    }
  });

  hostApi.runtime.setEntity(string.of("guard"), {
    textMap: {
      "state": string.of("idle")
    }
  });
};
