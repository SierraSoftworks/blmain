FROM mcr.microsoft.com/dotnet/core/sdk:3.1.411

ADD ./ /src
WORKDIR /src

RUN dotnet publish -c release -o /out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
COPY --from=0 /out /app

WORKDIR /app
ENTRYPOINT [ "/app/BLMain" ]