#!/usr/bin/env bash

dotnet ef migrations add -p src/Broadway -s src/Broadway.Silo InitialCreate;
dotnet ef migrations script -p src/Broadway -s src/Broadway.Silo -o projections-db-schema.sql;
rm -rf src/Broadway/Migrations;
sed -i -e '/^\s*$/d' projections-db-schema.sql;
tail -n +6 projections-db-schema.sql | head -n -2 > tmp;
mv tmp projections-db-schema.sql;
