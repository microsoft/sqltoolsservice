#This is a docker file to generate a SQL server. It helps you to execute the integration tests and for development
#For more information about the possibility for SQL server on docker, check this site out: https://hub.docker.com/_/microsoft-mssql-server 

FROM mcr.microsoft.com/mssql/server:2019-latest 

ENV ACCEPT_EULA=Y 
ENV SA_PASSWORD=abcDEF123#
ENV MSSQL_PID=Developer
ENV MSSQL_TCP_PORT=1433 


WORKDIR /src 
RUN (/opt/mssql/bin/sqlservr --accept-eula & ) | grep -q "Service Broker manager has started" 

#To create the image, execute the following command in the directory of the docker file 
#docker build . -t sqlserver_sqltoolsservice --no-cache 

#To execute the docker container, use the following command 
#docker run -d -p 1433:1433 --name sqlserver sqlserver_sqltoolsservice 

#Now you have a working SQL server for development and testing purposes 