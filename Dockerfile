FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as a distinct layer
COPY App/App.csproj ./App/App.csproj
COPY Web/Web.csproj ./Web/Web.csproj
RUN dotnet restore Web

# Copy everything else and build
COPY App/. ./App/.
COPY Web/. ./Web/.
RUN dotnet publish Web -c Release -o out --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Web.dll"]