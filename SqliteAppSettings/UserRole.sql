CREATE TABLE IF NOT EXISTS "UserRole" (
  "UserName" TEXT NOT NULL,
  "Role" TEXT NOT NULL,
  PRIMARY KEY ("UserName", "Role")
);