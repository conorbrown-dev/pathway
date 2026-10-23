# Build context: backend/Pathway.Api
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Pathway.Api.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
# The aspnet image ships a non-root "app" user; never run the API as root.
USER $APP_UID
ENTRYPOINT ["dotnet", "Pathway.Api.dll"]
