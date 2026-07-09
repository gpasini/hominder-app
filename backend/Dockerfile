FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Hominder.Domain/Hominder.Domain.csproj Hominder.Domain/
COPY Hominder.Application/Hominder.Application.csproj Hominder.Application/
COPY Hominder.Infrastructure/Hominder.Infrastructure.csproj Hominder.Infrastructure/
COPY Hominder.Api/Hominder.Api.csproj Hominder.Api/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore Hominder.Api/Hominder.Api.csproj

COPY Hominder.Domain/ Hominder.Domain/
COPY Hominder.Application/ Hominder.Application/
COPY Hominder.Infrastructure/ Hominder.Infrastructure/
COPY Hominder.Api/ Hominder.Api/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish Hominder.Api/Hominder.Api.csproj -c Release -o /app --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Hominder.Api.dll"]
