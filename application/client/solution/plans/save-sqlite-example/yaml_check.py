import json, yaml

path = r"E:\workspace\xml-xsd2\application\client\solution\plans\save-sqlite-example\save.yaml"
raw = open(path, encoding="utf-8").read()

doc = yaml.safe_load(raw)
js = json.dumps(doc, separators=(",", ":"), ensure_ascii=False)

print("yaml file bytes : %d" % len(raw.encode()))
print("compact json    : %d bytes" % len(js))

# spot-check the parsed structure
cave = doc["sectorGrids"]["cave"]
print("parsed ok       : grid=%s cells=%d portals=%d entity=%s" % (
    list(doc["sectorGrids"]),
    len(cave["cells"]),
    len(cave["portals"]),
    doc["entities"]["node-1"]["number"],
))
print("side type       : %r (quoted -> str, good)" % cave["portals"][0]["a"]["side"])

# --- the footguns, demonstrated on real parses ---
print("---")
print("bare N          : %r  <- YAML 1.1 boolean!" % yaml.safe_load("side: N")["side"])
print("bare yes        : %r" % yaml.safe_load("flag: yes")["flag"])
print("bare timestamp  : %r  <- datetime object, not a string" % yaml.safe_load('t: 2026-10-16T12:00:00Z')["t"])
print("sexagesimal     : %r  <- 1:30 parses as the integer 90" % yaml.safe_load("v: 1:30")["v"])
print("comment in str  : %r  <- # starts a comment even unquoted" % yaml.safe_load("v: hello # world")["v"])
