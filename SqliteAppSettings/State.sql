CREATE TABLE IF NOT EXISTS "State" (
  "Name" TEXT NOT NULL,
  "ValueInt" integer NOT NULL DEFAULT 0,
  "ValueReal" real NOT NULL DEFAULT 0,
  "ValueText" TEXT,
  PRIMARY KEY ("Name")
);