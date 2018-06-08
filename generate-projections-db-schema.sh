#!/usr/bin/env bash

dotnet ef migrations add -p src/Broadway -s src/Broadway.Silo InitialCreate;
dotnet ef migrations script -p src/Broadway -s src/Broadway.Silo -o projections-db-schema.sql;
rm -rf src/Broadway/Migrations;
