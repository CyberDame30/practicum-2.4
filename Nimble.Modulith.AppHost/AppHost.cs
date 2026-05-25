var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume();

var usersDb = sqlServer.AddDatabase("usersdb");
var productsDb = sqlServer.AddDatabase("productsdb");
var customersDb = sqlServer.AddDatabase("customersdb");
var reportingDb = sqlServer.AddDatabase("reportingdb");

var papercut = builder.AddContainer("papercut", "changemakerstudiosus/papercut-smtp:latest")
    .WithEndpoint(25, 25, name: "smtp")
    .WithEndpoint(37408, 80, name: "ui");

builder.AddProject<Projects.Nimble_Modulith_Web>("webapi")
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5000")
    .WithReference(usersDb)
    .WithReference(productsDb)
    .WithReference(customersDb)
    .WithReference(reportingDb)
    .WithEnvironment("Email__SmtpServer", "localhost")
    .WithEnvironment("Email__SmtpPort", "25")
    .WaitFor(usersDb)
    .WaitFor(productsDb)
    .WaitFor(customersDb)
    .WaitFor(reportingDb)
    .WaitFor(papercut);

builder.Build().Run();