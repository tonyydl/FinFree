FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src/FinFree.Api

COPY FinFree.Api/FinFree.Api.csproj .
COPY FinFree.Api/ .
RUN dotnet publish FinFree.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "FinFree.Api.dll"]
