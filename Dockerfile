FROM mcr.microsoft.com/dotnet/core/aspnet:3.0.0-alpine3.10
COPY /deploy /
WORKDIR /Server
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]