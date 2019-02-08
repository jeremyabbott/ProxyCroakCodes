FROM microsoft/dotnet:2.1.7-aspnetcore-runtime-alpine3.7
COPY /deploy /
WORKDIR /Server
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]