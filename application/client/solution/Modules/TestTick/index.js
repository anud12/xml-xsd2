import { getInterestModule } from "./module/interest/interest";
import { interestUi } from "./module/interest/ui";
import { getLandModule } from "./module/land/land";
import { landUi } from "./module/land/ui";
import { getInventoryModule } from "./module/inventory/inventory";
import { inventoryUi } from "./module/inventory/ui";
import { getValueModule } from "./module/value/value";
import { valueUi } from "./module/value/ui";
const entry = (hostApi) => {
    const interest = getInterestModule(hostApi);
    const land = getLandModule(hostApi);
    const inventory = getInventoryModule(hostApi);
    const value = getValueModule(hostApi);
    const game = hostApi.runtime.setEntity({
        id: hostApi.runtime.string.of("game"),
        numberMap: {
            ...interest.entityNumberMap,
            ...value.entityNumberMap,
            [land.genKey]: hostApi.runtime.number.of(0),
        },
    });
    interest.initialize();
    land.initialize();
    value.initialize();
    interestUi(hostApi, game);
    landUi(hostApi);
    inventoryUi(hostApi);
    valueUi(hostApi, game);
};
export default entry;
