/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string, condition} = hostApi.runtime;

  hostApi.runtime.setEntity(string.of("guard"), {
    textMap: {
      "state": string.of("idle")
    }
  });

  hostApi.runtime.registerAction({
    name: "patrol",
    apply: () => {
      hostApi.runtime.log("___From module patrol action fired___");
    }
  });

  const guardBehavior = hostApi.runtime.autonomy({
    name: string.of("guard-behavior"),
    priority: [
      {
        label: "default",
        condition: () => condition.of(true),
        utility: [
          {
            label: "patrol",
            score: () => number.of(1),
            do: (ctx) => [ctx.action(string.of("patrol"))]
          }
        ]
      }
    ]
  });

  hostApi.runtime.setAutonomy(string.of("guard"), guardBehavior);
};
