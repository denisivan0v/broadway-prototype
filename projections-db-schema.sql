CREATE SEQUENCE "EntityFrameworkHiLoSequence" START WITH 1 INCREMENT BY 10 NO MINVALUE NO MAXVALUE NO CYCLE;
CREATE TABLE "Branches" (
    "Code" int4 NOT NULL,
    "DefaultCityCode" int8 NULL,
    "DefaultCountryCode" int4 NOT NULL,
    "DefaultLang" text NOT NULL,
    "IsDeleted" bool NOT NULL,
    "IsOnInfoRussia" bool NOT NULL,
    "NameLat" text NULL,
    CONSTRAINT "PK_Branches" PRIMARY KEY ("Code")
);
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
CREATE TABLE "BranchLocalizations" (
    "Id" int8 NOT NULL,
    "BranchCode" int4 NULL,
    "Lang" text NOT NULL,
    "Name" text NOT NULL,
    CONSTRAINT "PK_BranchLocalizations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BranchLocalizations_Branches_BranchCode" FOREIGN KEY ("BranchCode") REFERENCES "Branches" ("Code") ON DELETE RESTRICT
);
CREATE TABLE "CardsRubrics" (
    "RubricCode" int8 NOT NULL,
    "CardCode" int8 NOT NULL,
    "IsPrimary" bool NOT NULL,
    "SortingPosition" int4 NOT NULL,
    CONSTRAINT "PK_CardsRubrics" PRIMARY KEY ("RubricCode", "CardCode"),
    CONSTRAINT "FK_CardsRubrics_Cards_CardCode" FOREIGN KEY ("CardCode") REFERENCES "Cards" ("Code") ON DELETE CASCADE
);
CREATE TABLE "RubricsBranches" (
    "RubricCode" int8 NOT NULL,
    "BranchCode" int4 NOT NULL,
    CONSTRAINT "PK_RubricsBranches" PRIMARY KEY ("RubricCode", "BranchCode"),
    CONSTRAINT "FK_RubricsBranches_Rubrics_RubricCode" FOREIGN KEY ("RubricCode") REFERENCES "Rubrics" ("Code") ON DELETE CASCADE
);
CREATE TABLE "RubricLocalizations" (
    "Id" int8 NOT NULL,
    "CategoryCode" int8 NULL,
    "Lang" text NOT NULL,
    "Name" text NOT NULL,
    "RubricCode" int8 NULL,
    "SecondRubricCode" int8 NULL,
    "ShortName" text NULL,
    CONSTRAINT "PK_RubricLocalizations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RubricLocalizations_Categories_CategoryCode" FOREIGN KEY ("CategoryCode") REFERENCES "Categories" ("Code") ON DELETE RESTRICT,
    CONSTRAINT "FK_RubricLocalizations_Rubrics_RubricCode" FOREIGN KEY ("RubricCode") REFERENCES "Rubrics" ("Code") ON DELETE RESTRICT,
    CONSTRAINT "FK_RubricLocalizations_SecondRubrics_SecondRubricCode" FOREIGN KEY ("SecondRubricCode") REFERENCES "SecondRubrics" ("Code") ON DELETE RESTRICT
);
CREATE INDEX "IX_CardForERMRubrics_CardCode" ON "CardForERMRubrics" ("CardCode");
CREATE INDEX "IX_Localizations_CategoryCode" ON "Localizations" ("CategoryCode");
CREATE INDEX "IX_Localizations_RubricCode" ON "Localizations" ("RubricCode");
CREATE INDEX "IX_Localizations_SecondRubricCode" ON "Localizations" ("SecondRubricCode");
CREATE INDEX "IX_BranchLocalizations_BranchCode" ON "BranchLocalizations" ("BranchCode");
CREATE INDEX "IX_CardsRubrics_CardForERMCode" ON "CardsRubrics" ("CardForERMCode");
CREATE INDEX "IX_RubricLocalizations_CategoryCode" ON "RubricLocalizations" ("CategoryCode");
CREATE INDEX "IX_RubricLocalizations_RubricCode" ON "RubricLocalizations" ("RubricCode");
CREATE INDEX "IX_RubricLocalizations_SecondRubricCode" ON "RubricLocalizations" ("SecondRubricCode");
