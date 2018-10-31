FROM microsoft/dotnet:2.1-aspnetcore-runtime
COPY /deploy .
WORKDIR .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]