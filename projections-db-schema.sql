﻿CREATE TABLE "Categories" (
    "Code" bigserial NOT NULL,
    "IsDeleted" bool NOT NULL,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("Code")
);

CREATE TABLE "Rubrics" (
    "Code" bigserial NOT NULL,
    "IsCommercial" bool NOT NULL,
    "IsDeleted" bool NOT NULL,
    "SecondRubricCode" int8 NOT NULL,
    CONSTRAINT "PK_Rubrics" PRIMARY KEY ("Code")
);

CREATE TABLE "SecondRubrics" (
    "Code" bigserial NOT NULL,
    "CategoryCode" int8 NOT NULL,
    "IsDeleted" bool NOT NULL,
    CONSTRAINT "PK_SecondRubrics" PRIMARY KEY ("Code")
);

CREATE TABLE "Localizations" (
    "Id" bigserial NOT NULL,
    "CategoryCode" int8 NULL,
    "Lang" text NULL,
    "Name" text NULL,
    "RubricCode" int8 NULL,
    "SecondRubricCode" int8 NULL,
    "ShortName" text NULL,
    CONSTRAINT "PK_Localizations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Localizations_Categories_CategoryCode" FOREIGN KEY ("CategoryCode") REFERENCES "Categories" ("Code") ON DELETE RESTRICT,
    CONSTRAINT "FK_Localizations_Rubrics_RubricCode" FOREIGN KEY ("RubricCode") REFERENCES "Rubrics" ("Code") ON DELETE RESTRICT,
    CONSTRAINT "FK_Localizations_SecondRubrics_SecondRubricCode" FOREIGN KEY ("SecondRubricCode") REFERENCES "SecondRubrics" ("Code") ON DELETE RESTRICT
);

CREATE INDEX "IX_Localizations_CategoryCode" ON "Localizations" ("CategoryCode");

CREATE INDEX "IX_Localizations_RubricCode" ON "Localizations" ("RubricCode");

CREATE INDEX "IX_Localizations_SecondRubricCode" ON "Localizations" ("SecondRubricCode");

