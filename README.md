Check connection string in SqlDataClientStringPerformanceIssue/Program.cs prior to running app.

Program generates ~ 10 MB string and stores it into nvarchar(max) column (table is created if not exists), then it attempts to select it using ExecuteScalarAsync.

Method ExecuteScalarAsync is executing up to 10 minutes, probably because of DataReader creating multiple byte arrays for each portion of data received from SQL. There are lots of CPU consumption and GCs when executing the query.

Selecting text in SSMS occures in less than a second.
