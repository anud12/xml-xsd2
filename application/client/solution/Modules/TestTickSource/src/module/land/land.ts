import {HostApi} from "module_types/HostApi";
import {EntityReadOnlyExpression} from "module_types/dataExpression/Entity";
import {ContainerReadOnlyExpression} from "module_types/dataExpression/Contaier";
import {getLandSprites} from "./sprites";


export const getLandModule = (hostApi: HostApi) => {

  const { number, string } = hostApi.runtime;

  const landContainer = hostApi.runtime.setContainer({
    id: string.of("lands"),
    entities: [],
  });

  const landTick = hostApi.runtime.setEffect({
    name: string.of("land-tick"),
    reoccurAfterMs: () => {
      return hostApi.runtime.maybe.of(number.of(10));
    },
    apply: (context) => {
      context.getMutableEntityBy({
        filters: [(e) => e.id.isContainingExactly(string.of("game"))],
      }).map((game) => {
        const count = game.number_map.get("landCount").orElse(number.of(0));
        const next = count.sum(number.of(1));

        const land = hostApi.runtime.setEntity({
          id: string.of(next.toString()),
          textMap: {
            name: string.of("Land"),
          },
          numberMap: {
            cost: number.of(10),
          },
        });

        context.createEntity(land);

        context.getMutableContainersBy({
          filters: [(c) => c.id.isContainingExactly(string.of("lands"))],
        }).map((container) => {
          container.addEntity(land);
        });

        game.number_map.put("landCount", () => hostApi.runtime.maybe.of(next));
      });
    },
  });

  const initialize = () => {
    hostApi.runtime.emitEvent(landTick, {});
  };

  return {
    landContainer,
    landTick,
    initialize,
  };
};

export const landUi = (hostApi: HostApi, landContainer: ContainerReadOnlyExpression) => {
  const { string, number } = hostApi.runtime;
  const font = hostApi.ui.setFont({ file: "fonts/Ubuntu-Regular.ttf", size: 14, color: "#FFFFFF" });

  const track = hostApi.runtime.setAnimation(string.of("land-track"), {
    frames: [{ sprite: hostApi.ui.getSpritePNG("black_pixel.png") }],
    duration: number.of(1),
  });

  const portrait = hostApi.runtime.setAnimation(string.of("land-portrait"), {
    frames: getLandSprites(hostApi).map(value => ({sprite:value})),
    duration: number.of(1),
    loop: true,
  });

  hostApi.ui.window("land-list", {
    panel: {
      x: 100,
      y: 100,
      width: 200,
      height: 400,
      title: "Lands",
      background: {
        animation: track,
        fill: { width: "stretch" as const, height: "stretch" as const },
      },
    },
  }, [
    hostApi.ui.entityList("land-items", { container: landContainer },
      (entity) => [
        hostApi.ui.window(entity.id, {
          panel: {
            width: 180,
            height: 60,
            layout: "column" as const,
            background: {
              animation: portrait,
              fill: {
                width: "stretch",
                height: "stretch",
                keepAspectRatio:true,
              }
            }
          },
        }, [
          hostApi.ui.field(entity.id + ":name", {
            entity: entity,
            map: "text",
            name: "name",
            fallback: "Unknown",
          }, font),
          hostApi.ui.field(entity.id + ":cost", {
            entity: entity,
            map: "number",
            name: "cost",
            fallback: "0",
          }, font),
        ]),
      ]),
  ]);
};
