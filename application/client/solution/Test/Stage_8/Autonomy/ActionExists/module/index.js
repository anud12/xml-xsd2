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
      hostApi.runtime.log("___action-exists first action fired___");
    }
  });

  hostApi.runtime.autonomy({
    name: string.of("action-exists-behavior"),
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
              ctx.action(string.of("missing"))
            ]
          }
        ]
      }
    ]
  });
};
