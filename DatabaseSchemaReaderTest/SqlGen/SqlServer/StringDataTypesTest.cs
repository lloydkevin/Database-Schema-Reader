﻿using DatabaseSchemaReader.DataSchema;
using DatabaseSchemaReader.SqlGen.SqlServer;
#if !NUNIT
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestContext = System.Object;
#endif

namespace DatabaseSchemaReaderTest.SqlGen.SqlServer
{
    [TestClass]
    public class StringDataTypesTest
    {
        /*
        --Character--
        CHAR(n) 
        NCHAR(n) 
        VARCHAR(n | max) 
        NVARCHAR(n | max) 
        TEXT 
        NTEXT 
         */
        private readonly DataTypeWriter _typeWriter = new DataTypeWriter();
        private readonly DatabaseColumn _column = new DatabaseColumn { Nullable = true };

        [TestMethod]
        public void TestStringNVarChar()
        {
            //arrange
            _column.DbDataType = "NVARCHAR";
            _column.Length = 5;

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("NVARCHAR (5)", result);
        }

        [TestMethod]
        public void TestStringNVarChar2()
        {
            //arrange
            _column.DbDataType = "NVARCHAR2";
            _column.Length = 5;

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("NVARCHAR (5)", result);
        }

        [TestMethod]
        public void TestStringVarChar()
        {
            //arrange
            _column.DbDataType = "VARCHAR";
            _column.Length = 5;

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("VARCHAR (5)", result); //NB we've changed to unicode here
        }

        [TestMethod]
        public void TestStringVarChar2()
        {
            //arrange
            _column.DbDataType = "VARCHAR2";
            _column.Length = 5;

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("VARCHAR (5)", result);
        }

        [TestMethod]
        public void TestStringWithMaxLength()
        {
            //arrange
            _column.DbDataType = "NVARCHAR";
            _column.Length = -1;

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("NVARCHAR (MAX)", result);
        }

        [TestMethod]
        public void TestCharUnicode()
        {
            //arrange
            _column.DbDataType = "NCHAR";
            _column.Length = 5;

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("NCHAR (5)", result);
        }


        [TestMethod]
        public void TestChar()
        {
            //arrange
            _column.DbDataType = "CHAR";
            _column.Length = 20;

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("CHAR (20)", result);
        }

        [TestMethod]
        public void TestClob()
        {
            //arrange
            _column.DbDataType = "CLOB";

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("NVARCHAR (MAX)", result);
        }

        [TestMethod]
        public void TestNText()
        {
            //arrange
            _column.DbDataType = "NTEXT";

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("NTEXT", result);
        }

        [TestMethod]
        public void TestText()
        {
            //arrange
            _column.DbDataType = "TEXT";

            //act
            var result = _typeWriter.WriteDataType(_column);

            //assert
            Assert.AreEqual("TEXT", result);
        }

    }
}
