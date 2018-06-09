#!/bin/sh

dotnet ef migrations add -p src/Broadway -s src/Broadway.Silo InitialCreate;
dotnet ef migrations script -p src/Broadway -s src/Broadway.Silo -o projections-db-schema.sql;
rm -rf src/Broadway/Migrations;
sed -i '' -e '/^\s*$/d' projections-db-schema.sql;
tail -n +6 projections-db-schema.sql | sed -e '$d' | sed -e '$d' > tmp;
mv tmp projections-db-schema.sql;
