// SCHEDULED_CTX_JS Part 1: opening + entity getter

const SCHED_CTX_JS_P1: &str = r#"(function() {
    var fe = null;
    for (var eid in globalThis.__entityData) { fe = globalThis.__entityData[eid]; break; }
    globalThis.__context = {
        getEntityBy: function(filter) {
            return { map: function(cb) { if (!fe) return;
                cb({ getNumber: function(key) {
                    return { map: function(cb3) {
                        if (!fe.numberMap||fe.numberMap[key]===undefined)
                            return { orElse: function(d) { return d; }};
                        var r=cb3({ sum: function(s) {
                            fe.numberMap[key]=Number(fe.numberMap[key])+Number(s);
                        }, isLessOrEqualTo: function(t) {
                            return Number(fe.numberMap[key])<=Number(t);
                        }});
                        return r||{ orElse: function(d) { return d; }};
                    }};
                }, getText: function(key) {
                    return { ifPresent: function(cb2) {
                        if(fe.textMap&&fe.textMap[key]!==undefined){
                            cb2({ get: function() { return fe.textMap[key]; },
                                set: function(val) {
                                    if(!fe.textMap)fe.textMap={};
                                    fe.textMap[key]=val;
                                }});
                        }}
                    }}
                });
            }"#;

pub fn get_part1() -> &'static str { SCHED_CTX_JS_P1 }
