import {getInterestModule} from "./module/interest/interest";
import {interestUi} from "./module/interest/ui";
import {getLandModule, landUi} from "./module/land/land";

const entry: ModuleEntrypoint = (hostApi) => {

  const module = getInterestModule(hostApi);

  const game = hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("game"),
    numberMap: {
      ...module.entityNumberMap,
      landCount: hostApi.runtime.number.of(0),
    },
  });

  const land = getLandModule(hostApi);

  module.initialize();
  land.initialize();
  interestUi(hostApi, game);
  landUi(hostApi, land.landContainer);

};

export default entry;
