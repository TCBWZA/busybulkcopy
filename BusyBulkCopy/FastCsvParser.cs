﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using NLog;
using NLog.Fluent;


namespace BusyBulkCopy
{
    class FastCsvReader : BaseCsvReader
    {
        protected CsvParser theParser;

        public FastCsvReader(string aFileName, string aTable, string aDatabase, string aServer, string aSchema)
        {
            //theParser = new Microsoft.VisualBasic.FileIO.TextFieldParser(aFileName);
            theParser = new CsvParser(aFileName);
            //theParser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
            //theParser.SetDelimiters(",");

            theFileFields = theParser.ReadFields();

            getTableFields(aTable, aDatabase, aServer, aSchema);


            foreach (Field f in theTableFields)
            {
                int i = 0;
                foreach (string ff in theFileFields)
                {
                    if (ff == f.Name)
                    {
                        f.FileFieldPosition = i;
                    }
                    i++;
                }
            }
            rownum = 0;



        }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public override bool Read()
        {
            //while (!theParser.EndOfData)
            while (theParser.Read())
            {
                try
                {
                    theValues = theParser.ReadFields();
                }
                catch (Exception ex)
                {
                    logger.Warn("Skipped line " + ex.Message);
                }
                rownum++;

                return true;
            }
            return false;

        }


    }
}
