CREATE TABLE IF NOT EXISTS "Users" (
  "UserName" TEXT NOT NULL,
  "Password" TEXT,
  "CreateTime" integer NOT NULL,
  PRIMARY KEY ("UserName")
);
