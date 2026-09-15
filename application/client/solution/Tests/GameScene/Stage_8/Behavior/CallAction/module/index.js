/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string, condition} = hostApi.runtime;

  const patrol = hostApi.runtime.registerAction({
    name: string.of("patrol"),
    apply: () => {
      hostApi.runtime.log("___From module patrol action fired___");
    }
  });

  const guardBehavior = hostApi.runtime.registerBehavior({
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

  hostApi.runtime.setEntity(string.of("guard"), {
    textMap: {
      "state": string.of("idle")
    },
    behavior: guardBehavior.name
  });
};