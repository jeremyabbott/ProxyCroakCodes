FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.4-alpine3.9
COPY /deploy /
WORKDIR /Server
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]