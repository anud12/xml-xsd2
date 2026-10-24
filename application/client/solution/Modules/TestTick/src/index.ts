const entry: ModuleEntrypoint = (hostApi) => {
  const { string, number, maybe, condition } = hostApi.runtime;
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });

  const game = hostApi.runtime.setEntity({
    id: string.of("game"),
    numberMap: {
      progress: number.of(100),
    },
  });

  const tick = hostApi.runtime.setEffect({
    name: string.of("tick"),
    reoccurAfterMs: (context, executionCount, input, output) => {
      const current = Number(
        context.getEntityBy({
          filters: [(e) => e.id.isContainingExactly(string.of("game"))],
        }).map((el) =>
          el.number_map.get("progress").orElse(number.of(0))
        )
      );
      if (current <= 0) return maybe.none();
      return maybe.of(number.of(1));
    },
    apply: (context, output) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((entity) => {
        entity.number_map.put("progress", (current) =>
          current.map(v => v.sum(number.of(-1)))
        );
      });
    },
  });

  hostApi.runtime.emitEvent(tick, {});

  const fill = hostApi.runtime.setAnimation(string.of("fill"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("assets/loading_bar.png") }],
    duration: number.of(1),
  });

  const track = hostApi.runtime.setAnimation(string.of("track"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("black_pixel.png") }],
    duration: number.of(1),
  });

  const incrementEffect = hostApi.runtime.setEffect({
    name: string.of("increment-event"),
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((entity) => {
        entity.number_map.put("progress", (current) => current.map(c => c.sum(number.of(10))));
      });
    },
  });


  const increment = hostApi.runtime.setAction({
    name: string.of("increment"),
    apply: (ctx) => {
      ctx.emitEffect(incrementEffect, {});
    },
  });


  hostApi.ui.window("progress", {
    panel: {
      x: 600,
      y: 400,
      width: 500,
      height: 30,
    },
  }, [
    hostApi.ui.progress("progress-bar", {
      panel: {
        x: 0,
        y: 0,
        width: 500,
        height: 30,
      },
      progress: {
        value: (entity) => entity.number_map.get("progress").orElse(number.of(0)),
        max: (entity) => number.of(100),
        min: (entity) => number.of(0),
        entity: game,
        steps: 100,
        fill: {
          animation: fill,
          fill: { width: "tile" as const, height: "stretch" as const },
        },
        track: {
          animation: track,
          fill: { width: "tile" as const, height: "stretch" as const },
        },
      },
    }),
  ]);

  hostApi.ui.window("add-btn", {
    panel: {
      x: 600,
      y: 440,
      width: 100,
      height: 40,
      background: {
        animation: track,
        fill: { width: "stretch" as const, height: "stretch" as const },
      },
      onClick: (ctx) => {
        ctx.emitAction(increment);
      },
    },
  }, [
    hostApi.ui.field("add-label", { entity: game, map: "number", name: "progress", fallback: "0" }, font),
  ]);
};

export default entry;
