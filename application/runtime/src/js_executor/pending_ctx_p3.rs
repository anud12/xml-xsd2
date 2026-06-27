// PENDING_CTX_JS Part 3: map, emitEvent, closing

const PENDING_CTX_JS_P3: &str = r#",map:function(cb){
                    if(!fe)return;
                    cb({getText:function(key){
                        return{ifPresent:function(cb2){
                            if(fe.textMap&&fe.textMap[key]!==undefined){
                                cb2({get:function(){return fe.textMap[key];},
                                    set:function(val){
                                        if(!fe.textMap)fe.textMap={};
                                        fe.textMap[key]=val;
                                    }});
                            }}
                        }}});
                    }};
                }};
            }
        ,
        emitEvent: function(name, payload) {
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
                    prepared=target.prepare(globalThis.__context);
                if(prepared&&typeof prepared==='object'&&prepared.value){
                    if(typeof target.apply==='function')
                        target.apply(globalThis.__context,prepared);
                }
                cm=(prepared&&typeof prepared==='object'&&prepared.value)||false;
            }catch(e){}}
            var _s=[cm];
            return{ifTrue:function(cb){
                if(_s[0]&&typeof cb==='function'){cb();_s[0]=false;}
            },ifFalse:function(cb){
                if(!_s[0]&&typeof cb==='function')cb();
            }};
        }
    };
})()"#;

pub fn get_part3() -> &'static str { PENDING_CTX_JS_P3 }
