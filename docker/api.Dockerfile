FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/JevAsk.Core/ src/JevAsk.Core/
COPY src/JevAsk.Api/ src/JevAsk.Api/
COPY data/samples/ data/samples/
RUN dotnet publish src/JevAsk.Api/JevAsk.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
COPY data/samples/ /app/data/samples/
ENV ASPNETCORE_URLS=http://+:8080
ENV Data__SamplePath=/app/data/samples
ENV ConnectionStrings__Cache=Data Source=/data/jev-ask.db
EXPOSE 8080
ENTRYPOINT ["dotnet", "JevAsk.Api.dll"]
