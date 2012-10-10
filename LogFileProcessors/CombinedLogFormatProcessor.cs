using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using ADOX;
using Assimilated.Alfac.LogHandlers;

namespace Assimilated.Alfac.LogFileProcessors
{
    public class CombinedLogFormatProcessor : LogFileProcessor
    {
        #region SQL statement and Regex pattern
        private const string InsertSql =
            "INSERT INTO Access([RemoteHost],[RemoteIdentity],[RemoteUser],[Time],[Method],[URL],[Protocol],[Status],[BytesSent],[Referer],[UserAgent]) " +
            "VALUES(@RemoteHost,@RemoteIdentity,@RemoteUser,@Time,@Method,@URL,@Protocol,@Status,@BytesSent,@Referer,@UserAgent)";

        private const string RegexPattern = @"(\S+) (\S+) (\S+) \[([^:]+):(\d+:\d+:\d+) ([^\]]+)\] ""(\S+) (.+?) (\S+)[ ]*"" (\S+) (\S+) ""([^""]*?)"" ""([^""]*?)""";

        private readonly OleDbParameter _remoteHost = new OleDbParameter { IsNullable = true, ParameterName = "@RemoteHost" };
        private readonly OleDbParameter _remoteLogname = new OleDbParameter { IsNullable = true, ParameterName = "@RemoteIdentity" };
        private readonly OleDbParameter _remoteUser = new OleDbParameter { IsNullable = true, ParameterName = "@RemoteUser" };
        private readonly OleDbParameter _time = new OleDbParameter { IsNullable = true, ParameterName = "@Time" };
        private readonly OleDbParameter _method = new OleDbParameter { IsNullable = true, ParameterName = "@Method" };
        private readonly OleDbParameter _url = new OleDbParameter { IsNullable = true, ParameterName = "@URL" };
        private readonly OleDbParameter _protocol = new OleDbParameter { IsNullable = true, ParameterName = "@Protocol" };
        private readonly OleDbParameter _status = new OleDbParameter { IsNullable = true, DbType = DbType.Int16, ParameterName = "@Status" };
        private readonly OleDbParameter _bytesSent = new OleDbParameter { IsNullable = true, DbType = DbType.Int32, ParameterName = "@BytesSent" };
        private readonly OleDbParameter _referer = new OleDbParameter { IsNullable = true, ParameterName = "@Referer" };
        private readonly OleDbParameter _userAgent = new OleDbParameter { IsNullable = true, ParameterName = "@UserAgent" };

        #endregion

        public override string TableName { get { return "Access"; } }
        public override string Name { get { return "Combined Log Format"; } }
        public override LogFileType LogFileType { get { return LogFileType.CombinedLogFormat; } }
        protected override string InsertSqlExpression { get { return InsertSql; } }
        protected override string TokenizerRegexPattern { get { return RegexPattern; } }

        public override ADOX.Table GetTable()
        {
            var table = new ADOX.Table();
            table.Name = TableName;
            table.Columns.Append("RemoteHost");
            table.Columns.Append("RemoteIdentity");
            table.Columns.Append("RemoteUser");
            table.Columns.Append("Time", DataTypeEnum.adDate);
            table.Columns.Append("Method");
            table.Columns.Append("URL", DataTypeEnum.adLongVarWChar);
            table.Columns.Append("Protocol");
            table.Columns.Append("Status", DataTypeEnum.adSmallInt);
            table.Columns.Append("BytesSent", DataTypeEnum.adInteger);
            table.Columns.Append("Referer", DataTypeEnum.adLongVarWChar);
            table.Columns.Append("UserAgent", DataTypeEnum.adLongVarWChar);

            // allow all columns to be empty
            foreach (ADOX.Column column in table.Columns)
            {
                column.Attributes = ColumnAttributesEnum.adColNullable;
            }

            var rhindex = new ADOX.Index();
            rhindex.Name = "RemoteHost index";
            rhindex.IndexNulls = AllowNullsEnum.adIndexNullsIgnore;
            rhindex.Columns.Append("RemoteHost");
            table.Indexes.Append(rhindex);

            var mindex = new ADOX.Index();
            mindex.Name = "Method index";
            mindex.IndexNulls = AllowNullsEnum.adIndexNullsIgnore;
            mindex.Columns.Append("Method");
            table.Indexes.Append(mindex);

            var tindex = new ADOX.Index();
            tindex.Name = "Time index";
            tindex.IndexNulls = AllowNullsEnum.adIndexNullsIgnore;
            tindex.Columns.Append("Time", DataTypeEnum.adDate);
            table.Indexes.Append(tindex);

            var sindex = new ADOX.Index();
            sindex.Name = "Status index";
            sindex.IndexNulls = AllowNullsEnum.adIndexNullsIgnore;
            sindex.Columns.Append("Status", DataTypeEnum.adSmallInt);
            table.Indexes.Append(sindex);

            return table;
        }

        protected override void SetSqlParamaterValues(GroupCollection groups)
        {
            // Assign captured values
            _remoteHost.Value = groups[1].Value != "-" ? groups[1].Value : string.Empty;
            _remoteLogname.Value = groups[2].Value != "-" ? groups[2].Value : string.Empty;
            _remoteUser.Value = groups[3].Value != "-" ? groups[3].Value : string.Empty;
            _time.Value = DateTime.Parse(string.Format("{0} {1} {2}", groups[4].Value, groups[5].Value, groups[6].Value));
            _method.Value = groups[7].Value;
            _url.Value = groups[8].Value;
            _protocol.Value = groups[9].Value;
            _status.Value = groups[10].Value != "-" ? groups[10].Value : string.Empty;
            _bytesSent.Value = groups[11].Value != "-" ? groups[11].Value : string.Empty;
            _referer.Value = groups[12].Value != "-" ? groups[12].Value : string.Empty;
            _userAgent.Value = groups[13].Value != "-" ? groups[13].Value : string.Empty;
        }

        protected override void AddSqlParamters(OleDbCommand cmd)
        {
            cmd.Parameters.Add(_remoteHost);
            cmd.Parameters.Add(_remoteLogname);
            cmd.Parameters.Add(_remoteUser);
            cmd.Parameters.Add(_time);
            cmd.Parameters.Add(_method);
            cmd.Parameters.Add(_url);
            cmd.Parameters.Add(_protocol);
            cmd.Parameters.Add(_status);
            cmd.Parameters.Add(_bytesSent);
            cmd.Parameters.Add(_referer);
            cmd.Parameters.Add(_userAgent);
        }
    }
}
