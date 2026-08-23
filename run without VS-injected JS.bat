dotnet build --configuration Release
set ASPNETCORE_ENVIRONMENT=Production
dotnet run --configuration Release --no-launch-profile --project OperaLegacyLab.Web
pause