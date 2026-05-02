FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

COPY Minify.sln .
COPY src/Minify.API/Minify.API.csproj                         src/Minify.API/
COPY src/Minify.Application/Minify.Application.csproj         src/Minify.Application/
COPY src/Minify.Communication/Minify.Communication.csproj     src/Minify.Communication/
COPY src/Minify.Domain/Minify.Domain.csproj                   src/Minify.Domain/
COPY src/Minify.Infrastructure/Minify.Infrastructure.csproj   src/Minify.Infrastructure/

RUN dotnet restore src/Minify.API/Minify.API.csproj

COPY src/ src/
RUN dotnet publish src/Minify.API/Minify.API.csproj \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S minify && adduser -S minify -G minify
USER minify

COPY --from=build --chown=minify:minify /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Minify.API.dll"]
