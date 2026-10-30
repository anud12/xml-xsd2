-- save.db — SQLite save format (format v1, fully normalized)
--
-- Build:  sqlite3 save.db < save.sql
-- Open:   any SQLite viewer (DB Browser for SQLite, SQLite Studio, DBeaver)
--
-- One file = one save slot. JSON appears in exactly two columns, both
-- genuinely opaque JS data: scheduled_effects.payload and active_plans.steps.
-- Everything else is relational.
--
-- Container rows are fully normalized. The runtime's row format is a closed
-- field set (built in js_host_api/script_panel_entity.rs + script_rest.rs):
--   { id, entities[], textMap, numberMap, getX, getY, getSpanX, getSpanY,
--     sizeX{value,outOfBounds}, sizeY{value,outOfBounds}, alignment, xKey, yKey }
-- getX/getY/getSpanX/getSpanY are NOT functions in the row: evalPositionFn
-- evaluates them against every entity at declaration time and stores a
-- {entityId: value} snapshot map — hence container_positions.

PRAGMA page_size = 512;
PRAGMA user_version = 1;    -- save format version; bump + migrate on change

-- ── meta ────────────────────────────────────────────────────────────────────
-- Exactly one row: save identity + sim clock.
CREATE TABLE meta (
  saved_at_utc   TEXT    NOT NULL,
  module_id      TEXT    NOT NULL,
  module_name    TEXT    NOT NULL,
  module_version TEXT    NOT NULL,
  elapsed_time   INTEGER NOT NULL
);
INSERT INTO meta VALUES
  ('2026-10-16T12:00:00Z', 'main-module', 'MainModule', '0.1.0', 12345);

-- ── entity values (EAV: one row per entity/key) ─────────────────────────────
CREATE TABLE entity_text (
  entity_id TEXT NOT NULL,
  key       TEXT NOT NULL,
  value     TEXT NOT NULL,
  PRIMARY KEY (entity_id, key)
);
CREATE TABLE entity_number (
  entity_id TEXT NOT NULL,
  key       TEXT NOT NULL,
  value     REAL NOT NULL,
  PRIMARY KEY (entity_id, key)
);
INSERT INTO entity_text VALUES
  ('player', 'state', 'alive');
INSERT INTO entity_number VALUES
  ('node-1', 'column', 3),
  ('node-1', 'row',    2),
  ('player', 'health', 87.5),
  ('player', 'energy', 40);

-- ── containers (normalized) ─────────────────────────────────────────────────
CREATE TABLE containers (
  id         TEXT PRIMARY KEY,
  alignment  TEXT,
  x_key      TEXT,
  y_key      TEXT,
  size_x     REAL,
  size_x_oob TEXT,
  size_y     REAL,
  size_y_oob TEXT
);
CREATE TABLE container_entities (
  container_id TEXT NOT NULL,
  seq          INTEGER NOT NULL,     -- preserves array order
  entity_id    TEXT NOT NULL,
  PRIMARY KEY (container_id, seq)
);
CREATE TABLE container_text (
  container_id TEXT NOT NULL,
  key          TEXT NOT NULL,
  value        TEXT NOT NULL,
  PRIMARY KEY (container_id, key)
);
CREATE TABLE container_number (
  container_id TEXT NOT NULL,
  key          TEXT NOT NULL,
  value        REAL NOT NULL,
  PRIMARY KEY (container_id, key)
);
-- Position-function snapshot: field ∈ getX|getY|getSpanX|getSpanY,
-- value = fn(entity) evaluated at declaration time.
CREATE TABLE container_positions (
  container_id TEXT NOT NULL,
  field        TEXT    NOT NULL CHECK (field IN ('getX','getY','getSpanX','getSpanY')),
  entity_id    TEXT NOT NULL,
  value        REAL NOT NULL,
  PRIMARY KEY (container_id, field, entity_id)
);
INSERT INTO containers VALUES
  ('grid-1', 'center', 'column', 'row', 40, 'clamp', 40, 'clamp');
INSERT INTO container_entities VALUES
  ('grid-1', 0, 'node-1');
INSERT INTO container_positions VALUES
  ('grid-1', 'getX',     'node-1', 3),
  ('grid-1', 'getY',     'node-1', 2),
  ('grid-1', 'getSpanX', 'node-1', 10),
  ('grid-1', 'getSpanY', 'node-1', 10);

-- ── sector grids (normalized) ───────────────────────────────────────────────
CREATE TABLE sector_grids (
  grid_id TEXT PRIMARY KEY           -- keeps grid identity even when empty
);
CREATE TABLE sector_cells (
  grid_id   TEXT NOT NULL,
  x         INTEGER NOT NULL,
  y         INTEGER NOT NULL,
  container TEXT NOT NULL,
  PRIMARY KEY (grid_id, x, y)
);
CREATE TABLE sector_cell_edges (
  grid_id TEXT NOT NULL,
  sq_x    INTEGER NOT NULL,
  sq_y    INTEGER NOT NULL,
  side    TEXT NOT NULL CHECK (side IN ('N','E','S','W')),
  PRIMARY KEY (grid_id, sq_x, sq_y, side)
);
CREATE TABLE sector_cell_openings (
  grid_id TEXT NOT NULL,
  cell_x  INTEGER NOT NULL,
  cell_y  INTEGER NOT NULL,
  side    TEXT NOT NULL CHECK (side IN ('N','E','S','W')),
  start   INTEGER NOT NULL,
  length  INTEGER NOT NULL,
  PRIMARY KEY (grid_id, cell_x, cell_y, side)   -- ≤1 opening per edge
);
CREATE TABLE sector_portals (
  grid_id    TEXT NOT NULL,
  portal_id  TEXT NOT NULL,
  a_cell_x   INTEGER NOT NULL, a_cell_y INTEGER NOT NULL,
  a_side     TEXT NOT NULL CHECK (a_side IN ('N','E','S','W')),
  a_span     INTEGER NOT NULL, a_length INTEGER NOT NULL,
  b_cell_x   INTEGER NOT NULL, b_cell_y INTEGER NOT NULL,
  b_side     TEXT NOT NULL CHECK (b_side IN ('N','E','S','W')),
  b_span     INTEGER NOT NULL, b_length INTEGER NOT NULL,
  PRIMARY KEY (grid_id, portal_id)
);
INSERT INTO sector_grids VALUES ('cave');
INSERT INTO sector_cells VALUES
  ('cave', 0, 0, 'room-a'),
  ('cave', 1, 0, 'room-b');
INSERT INTO sector_cell_edges VALUES
  ('cave', 0, 0, 'N'),
  ('cave', 0, 0, 'E'),
  ('cave', 1, 0, 'W');
INSERT INTO sector_cell_openings VALUES
  ('cave', 0, 0, 'N', 13, 4),
  ('cave', 0, 0, 'E', 7,  4),
  ('cave', 1, 0, 'W', 7,  4);
INSERT INTO sector_portals VALUES
  ('cave', 'p-0', 0, 0, 'E', 3, 4, 1, 0, 'W', 3, 4);

-- ── entity areas (ordered polygon points) ───────────────────────────────────
CREATE TABLE entity_area_points (
  entity_id TEXT NOT NULL,
  area_id   TEXT NOT NULL,
  seq       INTEGER NOT NULL,
  x         REAL    NOT NULL,
  y         REAL    NOT NULL,
  PRIMARY KEY (entity_id, area_id, seq)
);
INSERT INTO entity_area_points VALUES
  ('player', 'collider', 0, 0,  0),
  ('player', 'collider', 1, 40, 0),
  ('player', 'collider', 2, 40, 30),
  ('player', 'collider', 3, 0,  30);

-- ── scheduled effects ───────────────────────────────────────────────────────
-- name is unique (state/scheduled.rs retains by name before push).
CREATE TABLE scheduled_effects (
  name                  TEXT PRIMARY KEY,
  payload               TEXT    NOT NULL DEFAULT '{}',  -- opaque: JS-defined
  next_exec_time        INTEGER NOT NULL,
  reoccurrence_interval INTEGER NOT NULL,
  execution_count       INTEGER NOT NULL
);
INSERT INTO scheduled_effects VALUES
  ('repeat', '{}', 12400, 100, 42);

-- ── active plans ────────────────────────────────────────────────────────────
CREATE TABLE active_plans (
  action_name   TEXT    NOT NULL,
  actor         TEXT    NOT NULL,
  steps         TEXT    NOT NULL,   -- opaque: heterogeneous step objects
  resume_at     INTEGER NOT NULL,
  interruptible INTEGER NOT NULL CHECK (interruptible IN (0, 1))
);
INSERT INTO active_plans VALUES
  ('move-to-cursor', 'node-1',
   '[{"type":"move","container":"grid-1","entity":"node-1","startX":0,"startY":0,"deltaX":3,"deltaY":2,"dist":3.6,"remX":0,"remY":0,"posX":3,"posY":2,"lastTick":12345,"speed":2}]',
   12345, 1);

VACUUM;

-- Example queries a save-inspector tool could run:
--   SELECT key, value FROM entity_number WHERE entity_id = 'node-1';
--   SELECT entity_id FROM container_entities WHERE container_id = 'grid-1';
--   SELECT container FROM sector_cells WHERE grid_id = 'cave' AND x = 0;
--   SELECT * FROM sector_portals WHERE grid_id = 'cave';
--   SELECT action_name, actor FROM active_plans WHERE interruptible = 1;
