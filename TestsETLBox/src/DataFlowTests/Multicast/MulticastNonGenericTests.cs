using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.DataFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ALE.ETLBoxTests.Fixtures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ALE.ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class MulticastNonGenericTests
    {
        public SqlConnectionManager Connection => Config.SqlConnection.ConnectionManager("DataFlow");
        public MulticastNonGenericTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        [Fact]
        public void SplitInto2Tables()
        {
            //Arrange
            TwoColumnsTableFixture sourceTable = new TwoColumnsTableFixture("Source");
            sourceTable.InsertTestData();
            TwoColumnsTableFixture dest1Table = new TwoColumnsTableFixture("Destination1");
            TwoColumnsTableFixture dest2Table = new TwoColumnsTableFixture("Destination2");

            DBSource<string[]> source = new DBSource<string[]>(Connection, "Source");
            DBDestination<string[]> dest1 = new DBDestination<string[]>(Connection, "Destination1");
            DBDestination<string[]> dest2 = new DBDestination<string[]>(Connection, "Destination2");

            //Act
            Multicast<string[]> multicast = new Multicast<string[]>();

            source.LinkTo(multicast);
            multicast.LinkTo(dest1);
            multicast.LinkTo(dest2);
            source.Execute();
            dest1.Wait();
            dest2.Wait();

            //Assert
            dest1Table.AssertTestData();
            dest2Table.AssertTestData();
        }

    }
}
