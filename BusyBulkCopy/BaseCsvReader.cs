﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using NLog;
using NLog.Fluent;
using System.Globalization;

namespace BusyBulkCopy
{

    class BaseCsvReader : IDataReader
    {

        protected string[] theFileFields;
        protected string[] theValues;
        protected int rownum;
        protected Field[] theTableFields;
        protected int theErrorCount = 0;

        public virtual Object GetValue(int i)
        {

            Field myField = theTableFields[i];
            if (myField.FileFieldPosition < 0)
            { return myField.GetNull(); }
            string myValue = (theValues[myField.FileFieldPosition]).Trim();

            if (myValue == "")
            { return myField.GetNull(); }

            switch (myField.DataType.ToLower())
            {
                case "nvarchar":
                case "varchar":
                case "char":
                    if (myField.length < 0)
                    { return myValue; }
                    else if (myValue.Length > myField.length)
                    {
                        logger.Error(String.Format("Truncated data row {0}, field {1}, data type {2}({4}), data {3} ", rownum, myField.Name, myField.DataType, myValue, myField.length));
                        return myValue.Substring(0, (int)myField.length);
                    }
                    else
                    {
                        return myValue;

                    }
                case "smallint":
                    Int16 myInt16; double dddd16;
                    if (Int16.TryParse(myValue, out myInt16))
                    { return myInt16; }
                    else if (Double.TryParse(myValue, out dddd16))
                    { return double.IsNaN(dddd16) ? myField.GetNull() : Math.Round(dddd16); }
                    else
                    {
                        logger.Error(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                        return myField.GetNull();
                    }
                case "int":
                    int myInt; double dddd;
                    if (Int32.TryParse(myValue, out myInt))
                    { return myInt; }
                    else if (Double.TryParse(myValue, out dddd))
                    {
                        if (double.IsNaN(dddd))
                            return myField.GetNull();
                        else if (Int32.TryParse(Math.Round(dddd, 0).ToString(), out myInt))
                            return myInt;
                        else
                        {
                            logger.Error(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                            return myField.GetNull();
                        }
                    }
                    else
                    {
                        logger.Error(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                        return myField.GetNull();
                    }
                case "bigint":
                    Int64 myInt64;
                    if (Int64.TryParse(myValue, out myInt64))
                    { return myInt64; }
                    else if (Double.TryParse(myValue, out dddd))
                    {
                        if (double.IsNaN(dddd))
                            return myField.GetNull();
                        else if (Int64.TryParse(Math.Round(dddd, 0).ToString(), out myInt64))
                            return myInt64;
                        else
                        {
                            logger.Error(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                            return myField.GetNull();
                        }
                    }
                    else if (Double.TryParse(myValue, out dddd))
                    { return double.IsNaN(dddd) ? myField.GetNull() : Math.Round(dddd); }
                    else
                    {
                        logger.Error(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                        return myField.GetNull();
                    }
                case "numeric":
                    try
                    {
                        System.Data.SqlTypes.SqlDecimal d = Convert.ToDecimal(myValue);
                        if (d.Precision - d.Scale > myField.precision - myField.scale) // bigger before the comma
                        {
                            logger.Error(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                            return myField.GetNull();
                        }
                        else if (d.Scale > myField.scale) // bigger after the comma
                        {
                            // round it
                            if (Math.Abs(Math.Round(Convert.ToDecimal(myValue), myField.scale) - (decimal)d) != 0)
                            {
                                logger.Error(String.Format("Rounded invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                            }
                            return Math.Round(Convert.ToDecimal(myValue), myField.scale);
                        }
                        else
                        {
                            Decimal dd;
                            if (Decimal.TryParse(myValue, out dd))
                            { return dd; }
                            else
                            {
                                logger.Error(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                                return myField.GetNull();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} , message {4}", rownum, myField.Name, myField.DataType, myValue, ex.Message));
                        return myField.GetNull();
                    }
                case "bit":

                    switch (myValue.ToLower())
                    {
                        case "y":
                        case "1":
                        case "true":
                        case "t":
                            return true;
                        case "n":
                        case "0":
                        case "false":
                        case "f":
                            return false;
                        default:
                            logger.Error(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                            return myField.GetNull();
                    }
                case "float":
                case "real":
                    double ddd;
                    if (double.TryParse(myValue, out ddd))
                    {
                        if (!double.IsNaN(ddd)) return ddd;
                        else return myField.GetNull();
                    }
                    else
                    {
                        logger.Warn(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                        return myField.GetNull();
                    }
                case "datetime2":
                case "datetime":
                case "smalldatetime":
                    DateTime myDt;
                    CultureInfo enUK = new CultureInfo("en-UK");

                    if (DateTime.TryParseExact(myValue, "yyyy/MM/dd", enUK, DateTimeStyles.None, out myDt))
                    { return myDt; }
                    else if (DateTime.TryParseExact(myValue, "dd/MM/yyyy", enUK, DateTimeStyles.None, out myDt))
                    { return myDt; }
                    else
                    {
                        logger.Warn(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                        return myField.GetNull();
                    }
                default:
                    logger.Warn(String.Format("Skipped invalid data in row {0}, field {1}, data type {2}, data {3} ", rownum, myField.Name, myField.DataType, myValue));
                    return myField.GetNull();
            }
        }
        public virtual bool Read()
        {
            return false;
        }
        public BaseCsvReader()
        {

        }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        protected void log(string aString)
        {
            theErrorCount += 1;
            if (theErrorCount < 800) Console.WriteLine(aString);
        }

        protected void getTableFields(string aTable, string aDatabase, string aServer, string aSchema)
        {
            SqlConnection myConnection = new SqlConnection(String.Format("Data Source={0};Initial Catalog={1};Integrated Security=True", aServer, aDatabase));
            myConnection.Open();
            SqlCommand myCmd = myConnection.CreateCommand();
            myCmd.CommandText = String.Format(@"
SELECT COLUMN_NAME,
       DATA_TYPE,
       isnull(c.CHARACTER_MAXIMUM_LENGTH,0),
       c.IS_NULLABLE,
       convert(int,isnull(NUMERIC_PRECISION,0)),
       isnull(NUMERIC_SCALE,0)
FROM   INFORMATION_SCHEMA.COLUMNS c
WHERE  TABLE_NAME = '{0}'
       AND TABLE_SCHEMA = '{1}'
ORDER  BY ORDINAL_POSITION", aTable, aSchema);
            SqlDataReader myReader = myCmd.ExecuteReader();


            List<Field> myTableFields = new List<Field>();
            while (myReader.Read())
            {
                myTableFields.Add(new Field()
                {
                    Name = myReader.GetString(0),
                    DataType = myReader.GetString(1),
                    length = myReader.GetInt32(2),
                    nullable = myReader.GetString(3) == "YES" ? true : false,
                    precision = myReader.GetInt32(4),
                    scale = myReader.GetInt32(5),
                    FileFieldPosition = -1
                });
            }
            myReader.Close();

            theTableFields = myTableFields.ToArray();

        }


        public virtual int FieldCount { get { return theTableFields.Count(); } }


        protected string removeDoubleSpaces(string aStringWithALotOfSpaces)
        {
            string myNewString = aStringWithALotOfSpaces;
            while (myNewString.Contains("  "))
            {
                myNewString = myNewString.Replace("  ", " ");
            }
            return myNewString;
        }

        private bool theDataReaderOpen = true;

        #region "make the compiler happy"
        // this is crap to make the compiler happy
        public int Depth
        {
            get { return 0; }
        }
        public bool IsClosed
        {
            get { return !theDataReaderOpen; }
        }
        public int RecordsAffected
        {
            get { return -1; }
        }
        public void Close()
        {
            theDataReaderOpen = false;
        }
        public bool NextResult()
        {
            // The sample only returns a single resultset. However,
            // DbDataAdapter expects NextResult to return a value.
            return false;
        }
        public DataTable GetSchemaTable()
        {
            //$
            throw new NotSupportedException();
        }
        public String GetName(int i)
        {
            return "lenny";
        }
        public String GetDataTypeName(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public Type GetFieldType(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public int GetValues(object[] values)
        {
            throw new NotSupportedException("not supported.");
        }
        public int GetOrdinal(string name)
        {
            throw new NotSupportedException("not supported.");
        }
        public object this[int i]
        {
            get { throw new NotSupportedException("not supported."); }
        }
        public object this[String name]
        {
            // Look up the ordinal and return 
            // the value at that position.
            get { return this[GetOrdinal(name)]; }
        }
        public bool GetBoolean(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public byte GetByte(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException("not supported.");
        }
        public char GetChar(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException("not supported.");
        }
        public Guid GetGuid(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public Int16 GetInt16(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public Int32 GetInt32(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public Int64 GetInt64(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public float GetFloat(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public double GetDouble(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public String GetString(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public Decimal GetDecimal(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public DateTime GetDateTime(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public IDataReader GetData(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        public bool IsDBNull(int i)
        {
            throw new NotSupportedException("not supported.");
        }
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    this.Close();
                }
                catch (Exception e)
                {
                    throw new SystemException("An exception of type " + e.GetType() +
                                              " was encountered while closing the TemplateDataReader.");
                }
            }
        }
        #endregion
    }
}
