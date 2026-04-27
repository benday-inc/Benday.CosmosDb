if (Test-Path ./docfx_project/api) { Get-ChildItem ./docfx_project/api -Filter *.yml -File | Remove-Item -Force }
docfx ./docfx_project/docfx.json