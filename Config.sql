CREATE TABLE IF NOT EXISTS "Config" (
  "Key1" text(50) NOT NULL,
  "Key2" text(50) NOT NULL,
  "Value" text(500),
  PRIMARY KEY ("Key1", "Key2")
);