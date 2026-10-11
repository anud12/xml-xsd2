// SCHEDULED_CTX_JS Part 1: opening + entity getter (getEntityBy with map, get)

const SCHED_CTX_JS_P1: &str = r#"(function() {
    var fe = null, feId = null;
    for (var eid in globalThis.__entityData) { fe = globalThis.__entityData[eid]; feId = eid; break; }
    globalThis.__context = {
        getEntityBy: function(filter) {
            return {
                map: function(cb) {
                    if (!fe) return;
                    cb({
                        getNumber: function(key) {
                            return {
                                map: function(cb3) {
                                    if (!fe.numberMap || fe.numberMap[key]===undefined)
                                        return { orElse: function(d) { return d; } };
                                    var r = cb3({
                                        sum: function(s) {
                                            fe.numberMap[key] = Number(fe.numberMap[key])+Number(s);
                                        },
                                        divide: function(d) {
                                            var nv=fe.numberMap[key], rem=Number(nv)%Number(d);
                                            return {
                                                isEqualTo: function(t) {
                                                    var eq=(rem===Number(t));
                                                    return {
                                                        ifTrue: function(cb) { if(eq&&typeof cb==='function')cb(); },
                                                        ifFalse: function(cb) { if(!eq&&typeof cb==='function')cb(); },
                                                        orElse: function(d) {
                                                            return eq?{value:true,ifTrue:function(cb){
                                                                if(typeof cb==='function')cb();
                                                            },ifFalse:function(){}}:d;
                                                        }
                                                    };
                                                }
                                            };
                                        },
                                        isLessOrEqualTo: function(t) {
                                            return Number(fe.numberMap[key])<=Number(t);
                                        }
                                    });
                                    return r||{ orElse: function(d) { return d; } };
                                }
                            };
                        },
                        getText: function(key) {
                            return {
                                ifPresent: function(cb2) {
                                    if(fe.textMap&&fe.textMap[key]!==undefined){
                                        cb2({
                                            get: function() { return fe.textMap[key]; },
                                            set: function(val) {
                                                if(!fe.textMap)fe.textMap={};
                                                fe.textMap[key]=val;
                                            }
                                        });
                                    }
                                }
                            };
                        },
                        getEntitiesInsideArea: function(areaName) {
                            var areaMap = (globalThis.__insideArea && globalThis.__insideArea[feId]) || {};
                            var ids = areaMap[areaName] || [];
                            return {
                                forEach: function(cb) {
                                    for (var i = 0; i < ids.length; i++) {
                                        cb({ getId: function() { return ids[i]; } });
                                    }
                                },
                                size: function() { return ids.length; }
                            };
                        }
                    });
                },
                get: function(index) {
                    if(!fe) return {map:function(cb){return{orElse:function(d){return d;}};},flatMap:function(fn){return{orElse:function(d){return d;}};}};
                    var fk=null;
                    var ee={
                        getNumber:function(key){
                            fk=key;
                            return{
                                map:function(cb3){
                                    if(!fe.numberMap||fe.numberMap[key]===undefined)
                                        return{orElse:function(d){return d;}};
                                    var r=cb3({
                                        sum:function(s){fe.numberMap[key]=Number(fe.numberMap[key])+Number(s);},
                                        divide:function(d){
                                            var nv=fe.numberMap[key],rem=Number(nv)%Number(d);
                                            return{isEqualTo:function(t){
                                                var eq=(rem===Number(t));
                                                return{
                                                    ifTrue:function(cb){if(eq&&typeof cb==='function')cb();},
                                                    ifFalse:function(cb){if(!eq&&typeof cb==='function')cb();},
                                                    orElse:function(d){return eq?{value:true,ifTrue:function(cb){if(typeof cb==='function')cb();},ifFalse:function(){}}:d;}
                                                };
                                            }};
                                        },
                                        isLessOrEqualTo:function(t){return Number(fe.numberMap[key])<=Number(t);},
                                        modulo:function(d){
                                            var rem=Number(fe.numberMap[key])%Number(d);
                                            return{isEqualTo:function(t){
                                                var eq=(rem===Number(t));
                                                return{
                                                    ifTrue:function(cb){if(eq&&typeof cb==='function')cb();},
                                                    ifFalse:function(cb){if(!eq&&typeof cb==='function')cb();},
                                                    orElse:function(d){return eq?{value:true,ifTrue:function(cb){if(typeof cb==='function')cb();},ifFalse:function(){}}:d;}
                                                };
                                            }};
                                        }
                                    });
                                    return r||{orElse:function(d){return d;}};
                                }
                            };
                        },
                        getText:function(key){
                            return{ifPresent:function(cb){
                                if(fe.textMap&&fe.textMap[key]!==undefined){
                                    cb({get:function(){return fe.textMap[key];},
                                        set:function(val){
                                            if(!fe.textMap)fe.textMap={};
                                            fe.textMap[key]=val;
                                        }});
                                }
                            }};
                        }
                    };
                    return {
                        map: function(cb) {
                            cb(ee);
                            return {orElse:function(d){return d;}};
                        },
                        flatMap: function(fn) {
                            var result=fn(ee);
                            return{
                                map:function(cb){
                                    if(result&&typeof result.map==='function')
                                        return result.map(cb);
                                    return{orElse:function(d){return d;}};
                                },
                                orElse:function(d){return d;},
                                isCondition:function(condFn){
                                    var nv=fe.numberMap&&fk!==null?fe.numberMap[fk]:0;
                                    var nw={isLessOrEqualTo:function(t){return Number(nv)<Number(t);}};
                                    var it=condFn(nw);
                                    return{getOnTrueOrFalse:function(tv,fv){return it?tv:fv;}};
                                }
                            };
                        }
                    };
                }
            };
        },
        emitEvent: function(name, payload) {
            globalThis.__pendingEffects=globalThis.__pendingEffects||[];
            globalThis.__pendingEffects.push(name);
            var target=null,evs=globalThis.__registeredEvents||[];
            for(var i=0;i<evs.length;i++){
                if(evs[i]&&evs[i].name===name){target=evs[i];break;}
            }
            var _c=false;
            if(target){try{
                var cp=null;
                if(typeof target.prepare==='function')
                    cp=target.prepare(globalThis.__context);
                var hasVal=cp&&typeof cp==='object'&&'value' in cp;
                if(hasVal){
                    _c=!!cp.value;
                    if(typeof target.apply==='function')
                        target.apply(globalThis.__context,cp);
                }
            }catch(e){}}
            var _s=[_c];
            return{
                ifTrue:function(cb){if(_s[0]&&typeof cb==='function'){cb();_s[0]=false;}},
                ifFalse:function(cb){if(!_s[0]&&typeof cb==='function')cb();}
            };
        }
    };
})()"#;

pub fn get_part1() -> &'static str { SCHED_CTX_JS_P1 }
pub fn get_part2() -> &'static str { "" }
pub fn get_part3() -> &'static str { "" }
