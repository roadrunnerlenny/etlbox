using ETLBox.ConnectionManager;
using ETLBox.DataFlow;
using ETLBox.Helper;
using ETLBoxTests.Fixtures;
using CsvHelper.Configuration.Attributes;
using System;
using System.IO;
using Xunit;
using ETLBox.SqlServer;
using ETLBox.Csv;
using ETLBoxTests.Helper;

namespace ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class CsvDestinationConfigurationTests
    {
        public SqlConnectionManager SqlConnection => Config.SqlConnection.ConnectionManager("DataFlow");
        public CsvDestinationConfigurationTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public class MySimpleRow
        {
            [Index(1)]
            public int Col1 { get; set; }
            [Index(2)]
            public string Col2 { get; set; }

        }

        [Fact]
        public void DisableHeader()
        {
            //Arrange
            TwoColumnsTableFixture s2c = new TwoColumnsTableFixture("CsvSourceNoHeader");
            s2c.InsertTestData();
            DbSource<MySimpleRow> source = new DbSource<MySimpleRow>(SqlConnection, "CsvSourceNoHeader");

            //Act
            CsvDestination<MySimpleRow> dest = new CsvDestination<MySimpleRow>("./ConfigurationNoHeader.csv");
            dest.Configuration.HasHeaderRecord = false;
            source.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            Assert.Equal(File.ReadAllText("./ConfigurationNoHeader.csv"),
                File.ReadAllText("res/CsvDestination/TwoColumnsNoHeader.csv"));
        }


    }
}
