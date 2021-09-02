git clean -fdx . 
docfx init -q
copy /y docfx.json docfx_project
docfx --debugoutput build docfx_project\docfx.json --serve