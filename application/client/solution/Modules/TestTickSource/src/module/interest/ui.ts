import {HostApi} from "module_types/HostApi";
import {EntityReadOnlyExpression} from "../../../../../../../module_types/dataExpression/Entity";
import {getInterestModule} from "./interest";

export const interestUi = (hostApi:HostApi, game:EntityReadOnlyExpression) => {

  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });

  const module = getInterestModule(hostApi);

  const fill = hostApi.runtime.setAnimation(hostApi.runtime.string.of("fill"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("assets/loading_bar.png") }],
    duration: hostApi.runtime.number.of(1),
  });

  const track = hostApi.runtime.setAnimation(hostApi.runtime.string.of("track"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("black_pixel.png") }],
    duration: hostApi.runtime.number.of(1),
  });


  const incrementEffect = hostApi.runtime.setEffect({
    name: hostApi.runtime.string.of("increment-effect"),
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(hostApi.runtime.string.of("game"))],
      }).map((entity) => {
        entity.number_map.put(module.propertyKey, (current) => current.map(c => c.sum(hostApi.runtime.number.of(10))));
      });
    },
  });


  const increment = hostApi.runtime.setAction({
    name: hostApi.runtime.string.of("increment"),
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
        value: (entity) => entity.number_map.get(module.propertyKey).orElse(hostApi.runtime.number.of(0)),
        max: (entity) => module.maxValue,
        min: (entity) => hostApi.runtime.number.of(0),
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
    hostApi.ui.field("add-label", { entity: game, map: "number", name: module.propertyKey, fallback: "0" }, font),
  ]);
}