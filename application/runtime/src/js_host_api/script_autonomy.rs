pub fn host_api_script_autonomy() -> &'static str {
    r#"autonomy(definition) {
        if (!definition || typeof definition !== 'object') {
            throw new Error('autonomy: missing definition');
        }
        var resolvedName = typeof definition.name === 'object'
            ? definition.name.value : definition.name;
        if (typeof resolvedName !== 'string' || resolvedName === '') {
            throw new Error('autonomy: missing name');
        }
        globalThis.__autonomyDefinitions =
            globalThis.__autonomyDefinitions || {};
        if (resolvedName in globalThis.__autonomyDefinitions) {
            throw new Error('autonomy: duplicate name '
                + resolvedName);
        }
        function checkUtilityRule(rule, owner) {
            if (!rule || typeof rule !== 'object'
                || typeof rule.label !== 'string') {
                throw new Error('autonomy: utility rule missing label in '
                    + owner);
            }
            if (typeof rule.score !== 'function') {
                throw new Error('autonomy: ' + rule.label
                    + ' missing score in ' + owner);
            }
            if (typeof rule.do !== 'function') {
                throw new Error('autonomy: ' + rule.label
                    + ' missing do in ' + owner);
            }
            var doCtx = {
                action: function(name, payload) {
                    return { action: name, payload: payload };
                },
                wait: function(duration) {
                    return { wait: duration };
                }
            };
            var steps = rule.do(doCtx);
            if (!Array.isArray(steps)) {
                throw new Error('autonomy: ' + rule.label
                    + ' do must return a step array in ' + owner);
            }
            var registered = globalThis.__registeredActions || [];
            for (var s = 0; s < steps.length; s++) {
                var st = steps[s];
                if (!st || typeof st !== 'object'
                    || (st.action === undefined && st.wait === undefined)) {
                    throw new Error(
                        'autonomy: invalid step in ' + rule.label);
                }
                if (st.action !== undefined) {
                    var actionName = typeof st.action === 'object'
                        ? st.action.value : st.action;
                    var found = false;
                    for (var a = 0; a < registered.length; a++) {
                        var act = registered[a];
                        if (act && typeof act === 'object'
                            && act.name === actionName) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        throw new Error('autonomy: action '
                            + actionName + ' not registered in '
                            + rule.label);
                    }
                }
            }
            rule.steps = steps;
        }
        function checkPriorityRule(rule, owner) {
            if (!rule || typeof rule !== 'object'
                || typeof rule.label !== 'string') {
                throw new Error('autonomy: priority rule missing label in '
                    + owner);
            }
            if (typeof rule.condition !== 'function') {
                throw new Error('autonomy: ' + rule.label
                    + ' missing condition in ' + owner);
            }
            if (!Array.isArray(rule.utility)
                || rule.utility.length === 0) {
                throw new Error('autonomy: ' + rule.label
                    + ' utility must be a non-empty array in ' + owner);
            }
            for (var u = 0; u < rule.utility.length; u++) {
                checkUtilityRule(rule.utility[u], rule.label);
            }
        }
        if (Array.isArray(definition.priority)) {
            if (definition.priority.length === 0) {
                throw new Error(
                    'autonomy: priority must be a non-empty array');
            }
            for (var p = 0; p < definition.priority.length; p++) {
                checkPriorityRule(definition.priority[p], 'priority');
            }
        } else if (Array.isArray(definition.utility)) {
            if (definition.utility.length === 0) {
                throw new Error(
                    'autonomy: utility must be a non-empty array');
            }
            for (var u = 0; u < definition.utility.length; u++) {
                checkUtilityRule(definition.utility[u], 'utility');
            }
        } else {
            throw new Error(
                'autonomy: definition must declare priority or utility');
        }
        globalThis.__autonomyDefinitions[resolvedName] = definition;
        return { name: resolvedName };
    },
    setAutonomy(entityId, autonomy) {
        var resolvedId = typeof entityId === 'object'
            ? entityId.value : entityId;
        if (typeof resolvedId !== 'string' || resolvedId === '') {
            throw new Error('setAutonomy: missing entity id');
        }
        if (!autonomy || typeof autonomy !== 'object'
            || typeof autonomy.name !== 'string') {
            throw new Error('setAutonomy: not an autonomy handle');
        }
        globalThis.__autonomies = globalThis.__autonomies || {};
        globalThis.__autonomies[resolvedId] = autonomy;
    }"#
}
