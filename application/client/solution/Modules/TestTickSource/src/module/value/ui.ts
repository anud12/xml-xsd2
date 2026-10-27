import {HostApi} from "module_types/HostApi";
import {EntityReadOnlyExpression} from "../../../../../../../module_types/dataExpression/Entity";
import {getValueModule} from "./value";

export const valueUi = (hostApi: HostApi, game: EntityReadOnlyExpression) => {

  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 16, color: "#FFFFFF" });

  const module = getValueModule(hostApi);

  const fill = hostApi.runtime.setAnimation(hostApi.runtime.string.of("value-fill"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("assets/loading_bar.png") }],
    duration: hostApi.runtime.number.of(1),
  });

  const track = hostApi.runtime.setAnimation(hostApi.runtime.string.of("value-track"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("black_pixel.png") }],
    duration: hostApi.runtime.number.of(1),
  });

  hostApi.ui.window("value-progress", {
    panel: {
      x: 600,
      y: 400,
      width: 500,
      height: 30,
    },
  }, [
    hostApi.ui.progress("value-bar", {
      panel: {
        x: 0,
        y: 0,
        width: 500,
        height: 30,
      },
      progress: {
        value: (entity) => entity.number_map.get(module.valueKey).orElse(hostApi.runtime.number.of(0)),
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

  hostApi.ui.window("value-label", {
    panel: {
      x: 600,
      y: 440,
      width: 100,
      height: 40,
      background: {
        animation: track,
        fill: { width: "stretch", height: "stretch" as const },
      },
    },
  }, [
    hostApi.ui.field("value-readout", { entity: game, map: "number", name: module.valueKey, fallback: "0" }, font),
  ]);
};
