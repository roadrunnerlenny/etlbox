using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.DataFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ALE.ETLBoxTests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace ALE.ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class DBSourceExceptionTests
    {
        public static SqlConnectionManager SqlConnection => Config.SqlConnection.ConnectionManager("DataFlow");

        public DBSourceExceptionTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        [Fact]
        public void UnknownTable()
        {
            //Arrange
            DBSource<string[]> source = new DBSource<string[]>(SqlConnection, "UnknownTable");
            MemoryDestination<string[]> dest = new MemoryDestination<string[]>();

            //Act & Assert
            Assert.Throws<ETLBoxException>(() =>
            {
                source.LinkTo(dest);
                source.Execute();
                dest.Wait();
            });
        }

        [Fact]
        public void UnknownTableViaTableDefinition()
        {
            //Arrange
            TableDefinition def = new TableDefinition("UnknownTable",
                new List<TableColumn>()
                {
                    new TableColumn("id", "INT")
                });
            DBSource<string[]> source = new DBSource<string[]>()
            {
                ConnectionManager = SqlConnection,
                SourceTableDefinition = def
            };
            MemoryDestination<string[]> dest = new MemoryDestination<string[]>();

            //Act & Assert
            Assert.Throws<Microsoft.Data.SqlClient.SqlException>(() =>
            {
                source.LinkTo(dest);
                source.Execute();
                dest.Wait();
            });
        }
    }
}
