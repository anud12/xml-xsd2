// PENDING_CTX_JS Part 1: opening + entity getter with number ops

const PENDING_CTX_JS_P1: &str = r#"(function() {
    var fe = null;
    for (var eid in globalThis.__entityData) { fe = globalThis.__entityData[eid]; break; }
    globalThis.__context = {
        getEntityBy: function(filter) {
            return { map: function(cb) { if (!fe) return;
                cb({ getNumber: function(key) {
                    return { map: function(cb3) {
                        if (!fe.numberMap || fe.numberMap[key]===undefined)
                            return { orElse: function(d) { return d; }};
                        var r = cb3({ sum: function(s) {
                            fe.numberMap[key] = Number(fe.numberMap[key])+Number(s);
                        }, divide: function(d) {
                            var nv=fe.numberMap[key], rem=Number(nv)%Number(d);
                            return { isEqualTo: function(t) {
                                var eq=(rem===Number(t));
                                return { ifTrue: function(cb) {
                                    if(eq&&typeof cb==='function')cb();
                                }, ifFalse: function(cb) {
                                    if(!eq&&typeof cb==='function')cb();
                                }, orElse: function(d) {
                                    return eq?{value:true,ifTrue:function(cb){
                                        if(typeof cb==='function')cb();
                                    },ifFalse:function(){}}:d;
                                }};
                            }};
                        }, isLessOrEqualTo: function(t) {
                            return Number(fe.numberMap[key])<=Number(t);
                        }});
                        return r||{ orElse: function(d) { return d; }};
                    }};
                }, getText: function(key) {
                    return { ifPresent: function(cb) {
                        if(fe.textMap&&fe.textMap[key]!==undefined){
                            cb({ get: function() { return fe.textMap[key]; },
                                set: function(val) {
                                    if(!fe.textMap)fe.textMap={};
                                    fe.textMap[key]=val;
                                }});
                        }}
                    }}
                });
            }"#;

pub fn get_part1() -> &'static str { PENDING_CTX_JS_P1 }
