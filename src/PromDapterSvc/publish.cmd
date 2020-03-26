rmdir /s /q bin\Release\netcoreapp3.1\win10-x64\publish
dotnet publish PromDapterSvc.csproj -c Release --self-contained --runtime win10-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
copy Prometheusmapping.yaml bin\Release\netcoreapp3.1\win10-x64\publish\