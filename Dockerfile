FROM mcr.microsoft.com/dotnet/sdk:9.0

ADD ./ /src
WORKDIR /src

RUN dotnet publish -c release -o /out

FROM mcr.microsoft.com/dotnet/runtime:8.0
COPY --from=0 /out /app

WORKDIR /app
ENTRYPOINT [ "/app/BLMain" ]