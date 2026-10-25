import {getInterestModule} from "./module/interest/interest";
import {interestUi} from "./module/interest/ui";

const entry: ModuleEntrypoint = (hostApi) => {


  const module = getInterestModule(hostApi);
  
  const game = hostApi.runtime.setEntity({
    id: hostApi.runtime.string.of("game"),
    numberMap: {
      ...module.entityNumberMap
    },
  });



  module.initialize();
  interestUi(hostApi, game);

};

export default entry;
