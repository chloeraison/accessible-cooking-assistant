-- schema.sql

-- recipes table: one row per recipe
CREATE TABLE IF NOT EXISTS recipes (
  recipe_id   SERIAL   PRIMARY KEY,
  name        TEXT     NOT NULL,
  description TEXT,
  servings    INT,
  prep_time   INTERVAL
);

-- ingredients table: many ingredients per recipe
CREATE TABLE IF NOT EXISTS ingredients (
  ingredient_id SERIAL PRIMARY KEY,
  recipe_id     INT      REFERENCES recipes(recipe_id) ON DELETE CASCADE,
  name          TEXT     NOT NULL,
  quantity      NUMERIC,
  unit          TEXT
);

-- steps table: many ordered steps per recipe
CREATE TABLE IF NOT EXISTS steps (
  step_id      SERIAL PRIMARY KEY,
  recipe_id    INT    REFERENCES recipes(recipe_id) ON DELETE CASCADE,
  step_number  INT    NOT NULL,
  instruction  TEXT   NOT NULL
);
