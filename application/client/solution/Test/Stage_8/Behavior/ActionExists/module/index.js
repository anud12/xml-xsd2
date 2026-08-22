/** @type {ModuleEntrypoint} */
export default (hostApi) => {
  const {number, string, condition} = hostApi.runtime;

  const first = hostApi.runtime.registerAction({
    name: string.of("first"),
    apply: () => {
      hostApi.runtime.log("___action-exists first action fired___");
    }
  });

  hostApi.runtime.registerBehavior({
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
              ctx.action(first.name),
              ctx.action(string.of("missing"))
            ]
          }
        ]
      }
    ]
  });
};