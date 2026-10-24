// PENDING_CTX_JS Part 1: opening + entity getter (getEntityBy delegates to
// the shared FilterArg evaluator installed by the host API).

const PENDING_CTX_JS_P1: &str = r#"(function() {
    globalThis.__prepareContext = {
        getEntityBy: function(filter) {
            return globalThis.__evalEntityFilter(filter);
        },
        emitEvent: function(name, payload) {
            if(name && typeof name==='object'){
                name=(typeof name.name==='string')?name.name
                    :((typeof name.value==='string')?name.value:String(name));
            }
            globalThis.__pendingEffects=globalThis.__pendingEffects||[];
            globalThis.__pendingEffects.push(name);
            var target=null,evs=globalThis.__registeredEvents||[];
            for(var i=0;i<evs.length;i++){
                if(evs[i]&&evs[i].name===name){target=evs[i];break;}
            }
            var cm=false;
            if(target){try{
                var prepared=null;
                if(typeof target.prepare==='function')
                    prepared=target.prepare(globalThis.__prepareContext);
                var hasVal=prepared&&typeof prepared==='object'&&'value' in prepared;
                if(hasVal){
                    cm=!!prepared.value;
                    if(typeof target.apply==='function')
                        target.apply(globalThis.__applyContext,prepared);
                }
            }catch(e){}}
            var _s=[cm];
            return{
                ifTrue:function(cb){if(_s[0]&&typeof cb==='function'){cb();_s[0]=false;}},
                ifFalse:function(cb){if(!_s[0]&&typeof cb==='function')cb();}
            };
        }
    };
    globalThis.__applyContext = {
        createEntity: function(o) {
            globalThis.__createdEntities = globalThis.__createdEntities || [];
            globalThis.__entityStore = globalThis.__entityStore || [];
            if (o && typeof o === 'object' && typeof o.firstName === 'string') {
                var e = {}; e[o.firstName]=o.firstName; globalThis.__entityStore.push(e);
                globalThis.__createdEntities.push(o.firstName);
            } else if (o && typeof o === 'object') {
                globalThis.__entityStore.push(o);
                globalThis.__createdEntities.push(JSON.stringify(o));
            } else {
                globalThis.__entityStore.push({textMap_name: String(o)});
                globalThis.__createdEntities.push(String(o));
            }
        },
        getMutableEntityBy: function(filter) {
            return globalThis.__evalEntityFilter(filter);
        }
    };
})()"#;

pub fn get_part1() -> &'static str { PENDING_CTX_JS_P1 }
pub fn get_part2() -> &'static str { "" }
pub fn get_part3() -> &'static str { "" }
