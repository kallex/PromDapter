rmdir /s /q ..\..\src\..\publish
dotnet publish PromDapterSvc.csproj -c Release --self-contained --runtime win10-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true -o ..\..\src\..\publish
copy Prometheusmapping.yaml ..\..\src\..\publish