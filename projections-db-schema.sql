﻿CREATE SEQUENCE "EntityFrameworkHiLoSequence" START WITH 1 INCREMENT BY 10 NO MINVALUE NO MAXVALUE NO CYCLE;
CREATE TABLE "Cards" (
    "Code" int8 NOT NULL,
    "BranchCode" int4 NOT NULL,
    "ClosedForAscertainment" bool NOT NULL,
    "CountryCode" int4 NULL,
    "FirmCode" int8 NOT NULL,
    "IsActive" bool NOT NULL,
    "IsDeleted" bool NOT NULL,
    "IsLinked" bool NOT NULL,
    "SortingPosition" int4 NOT NULL,
    "Address_BuildingPurposeCode" int4 NULL,
    "Address_TerritoryCode" int8 NULL,
    "Address_Text" text NULL,
    CONSTRAINT "PK_Cards" PRIMARY KEY ("Code")
);
CREATE TABLE "Categories" (
    "Code" int8 NOT NULL,
    "IsDeleted" bool NOT NULL,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("Code")
);
CREATE TABLE "Firms" (
    "Code" int8 NOT NULL,
    "BranchCode" int4 NOT NULL,
    "ClosedForAscertainment" bool NOT NULL,
    "CountryCode" int4 NULL,
    "IsActive" bool NOT NULL,
    "IsArchived" bool NOT NULL,
    "Name" text NULL,
    CONSTRAINT "PK_Firms" PRIMARY KEY ("Code")
);
CREATE TABLE "Rubrics" (
    "Code" int8 NOT NULL,
    "IsCommercial" bool NOT NULL,
    "IsDeleted" bool NOT NULL,
    "SecondRubricCode" int8 NOT NULL,
    CONSTRAINT "PK_Rubrics" PRIMARY KEY ("Code")
);
CREATE TABLE "SecondRubrics" (
    "Code" int8 NOT NULL,
    "CategoryCode" int8 NOT NULL,
    "IsDeleted" bool NOT NULL,
    CONSTRAINT "PK_SecondRubrics" PRIMARY KEY ("Code")
);
CREATE TABLE "CardForERMRubrics" (
    "RubricCode" int8 NOT NULL,
    "CardForERMCode" int8 NULL,
    "IsPrimary" bool NOT NULL,
    "SortingPosition" int4 NOT NULL,
    CONSTRAINT "PK_CardForERMRubrics" PRIMARY KEY ("RubricCode"),
    CONSTRAINT "FK_CardForERMRubrics_Cards_CardForERMCode" FOREIGN KEY ("CardForERMCode") REFERENCES "Cards" ("Code") ON DELETE RESTRICT
);
CREATE TABLE "Localizations" (
    "Id" int8 NOT NULL,
    "CategoryCode" int8 NULL,
    "Lang" text NOT NULL,
    "Name" text NOT NULL,
    "RubricCode" int8 NULL,
    "SecondRubricCode" int8 NULL,
    "ShortName" text NULL,
    CONSTRAINT "PK_Localizations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Localizations_Categories_CategoryCode" FOREIGN KEY ("CategoryCode") REFERENCES "Categories" ("Code") ON DELETE RESTRICT,
    CONSTRAINT "FK_Localizations_Rubrics_RubricCode" FOREIGN KEY ("RubricCode") REFERENCES "Rubrics" ("Code") ON DELETE RESTRICT,
    CONSTRAINT "FK_Localizations_SecondRubrics_SecondRubricCode" FOREIGN KEY ("SecondRubricCode") REFERENCES "SecondRubrics" ("Code") ON DELETE RESTRICT
);
CREATE INDEX "IX_CardForERMRubrics_CardForERMCode" ON "CardForERMRubrics" ("CardForERMCode");
CREATE INDEX "IX_Localizations_CategoryCode" ON "Localizations" ("CategoryCode");
CREATE INDEX "IX_Localizations_RubricCode" ON "Localizations" ("RubricCode");
CREATE INDEX "IX_Localizations_SecondRubricCode" ON "Localizations" ("SecondRubricCode");
