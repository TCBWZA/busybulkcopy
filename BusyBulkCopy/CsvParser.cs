﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace BusyBulkCopy
{
    class CsvParser
    {
        string theData;
        StreamReader theReader;
        public CsvParser(string aFileName)
        {
            theReader = new StreamReader(aFileName, detectEncodingFromByteOrderMarks: true);
        }
        public bool Read()
        {
            if (theReader.Peek() >= 0) { return true; }
            else { return false; }
        }
        public string[] ReadFields()
        {
            string myLine =
                theReader.ReadLine();
            int l = myLine.Length;
            List<string> myRow = new List<string>();
            int i = 0;
            while (i < l)
            {
                //quoted
                if (myLine[i] == '"')
                {
                    // skip quote
                    i++;
                    int start = i;

                    while (true)
                    {
                        if (i == l) { break; }

                        if (myLine[i] == '"')
                        {
                            //if (i == start) { break; }
                            if (myLine[i + 1] != '"' & myLine[i - 1] != '"')
                            { break; }
                        }
                        i++;

                    }
                    if (i == start)
                    { myRow.Add(""); }
                    else
                    {
                        myRow.Add(myLine.Substring(start, i - start).Replace("\"\"", "\""));
                    }
                    i++;
                }
                else
                {
                    int start = i;
                    while (i < l && myLine[i] != ',')
                        i++;
                    //add the value
                    myRow.Add(myLine.Substring(start, i - start));
                }
                i++;
            }

            return myRow.ToArray();

        }
    }
}
