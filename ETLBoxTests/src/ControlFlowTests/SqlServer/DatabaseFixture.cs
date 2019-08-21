﻿using ALE.ETLBox.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ALE.ETLBoxTests.SqlServer
{
    [CollectionDefinition("Sql Server ControlFlow")]
    public class ControlFlowCollectionClass : ICollectionFixture<DatabaseFixture> { }
    public class DatabaseFixture
    {
        public DatabaseFixture()
        {
            DatabaseHelper.RecreateDatabase(Config.SqlConnectionString("ControlFlow").DBName
                , Config.SqlConnectionString("ControlFlow"));
        }
    }

}
