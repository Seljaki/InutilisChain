FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["InutilisChain/InutilisChain.csproj", "InutilisChain/"]
RUN dotnet restore "InutilisChain/InutilisChain.csproj"
COPY . .
WORKDIR "/src/InutilisChain"
RUN dotnet build "InutilisChain.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InutilisChain.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InutilisChain.dll"]
