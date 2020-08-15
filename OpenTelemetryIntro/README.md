OpenTelemetry Integration

https://blog.macrosssoftware.com/index.php/2020/08/13/opentelemetry-integration/

* This code exports OpenTelemetry data to an instance of Jaeger. Follow [these
  instructions](https://www.jaegertracing.io/docs/1.18/getting-started/) to
  launch a container.

* This code calls SQL using the ConnectionString defined in
  [./WebService/appsettings.Development.json](./WebService/appsettings.Development.json).

    There is no SQL in the solution though, because the call is just to cause
    some telemetry.

    To get up and running, you can follow these steps...

    * Run some kind of Sql. MSSQL or LocalDB should be fine. The default is
      localhost.
    * Create a new database. The default is blogDB.
    * Create a dummy proc named sp_StoreQuery. You can use this script:

       ```sql
        CREATE PROCEDURE [dbo].[sp_StoreQuery]
            @Query nvarchar(max)
        AS
        BEGIN
            return 0
        END
       ```