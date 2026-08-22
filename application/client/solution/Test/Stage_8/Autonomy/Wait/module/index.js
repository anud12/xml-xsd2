/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string, condition} = hostApi.runtime;

  hostApi.runtime.setEntity(string.of("guard"), {
    textMap: {
      "state": string.of("idle")
    }
  });

  hostApi.runtime.registerAction({
    name: "first",
    apply: () => {
      hostApi.runtime.log("___wait-test first action fired___");
    }
  });

  hostApi.runtime.registerAction({
    name: "second",
    apply: () => {
      hostApi.runtime.log("___wait-test second action fired___");
    }
  });

  const guardBehavior = hostApi.runtime.autonomy({
    name: string.of("wait-behavior"),
    priority: [
      {
        label: "default",
        condition: () => condition.of(true),
        utility: [
          {
            label: "sequence",
            score: () => number.of(1),
            do: (ctx) => [
              ctx.action(string.of("first")),
              ctx.wait(number.of(2)),
              ctx.action(string.of("second"))
            ]
          }
        ]
      }
    ]
  });

  hostApi.runtime.setAutonomy(string.of("guard"), guardBehavior);
};
