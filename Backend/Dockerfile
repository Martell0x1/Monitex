FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY smart_home.csproj ./
RUN dotnet restore ./smart_home.csproj

COPY . ./
RUN dotnet publish ./smart_home.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5020
EXPOSE 5020

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "smart_home.dll"]
