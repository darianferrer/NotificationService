FROM mcr.microsoft.com/mssql/server:2017-latest-ubuntu
USER root
RUN mkdir -p /src
WORKDIR /src
COPY . /src
ENTRYPOINT /bin/bash ./Database/entrypoint.sh