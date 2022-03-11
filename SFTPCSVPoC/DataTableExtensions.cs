using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFTPCSVPoC
{
    public static class DataTableExtensions
    {
        public static string WriteToTSFFormat(this DataTable dataTable)
        {
            StringBuilder fileContent = new StringBuilder();

            foreach (var col in dataTable.Columns)
            {
                fileContent.Append(col.ToString() + "\t");
            }

            fileContent.Replace("\t", System.Environment.NewLine, fileContent.Length - 1, 1);

            foreach (DataRow dr in dataTable.Rows)
            {
                foreach (var column in dr.ItemArray)
                {
                    fileContent.Append("\"" + column.ToString() + "\"\t");
                }

                fileContent.Replace("\t", System.Environment.NewLine, fileContent.Length - 1, 1);
            }

            return fileContent.ToString();
        }
    }
}
