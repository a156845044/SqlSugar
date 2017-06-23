﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlSugar
{
    public class MySqlDbMaintenance : DbMaintenanceProvider
    {
        #region DML
        protected override string GetColumnInfosByTableNameSql
        {
            get
            {
                string sql = @"SELECT
                                    0 as TableId,
                                    TABLE_NAME as TableName, 
                                    column_name AS DbColumnName,
                                    left(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)-1)   AS DataType,
                                    SUBSTRING(COLUMN_TYPE,LOCATE('(',COLUMN_TYPE)+1,LOCATE(')',COLUMN_TYPE)-LOCATE('(',COLUMN_TYPE)-1)  AS Length,
                                    column_default  AS  `DefaultValue`,
                                    column_comment  AS  `ColumnDescription`,
                                    CASE WHEN COLUMN_KEY = 'PRI'
                                    THEN 1 ELSE 0 END AS `IsPrimaryKey`,
                                    CASE WHEN is_nullable = 'YES'
                                    THEN 1 ELSE 0 END AS `IsNullable`
                                    FROM
                                    Information_schema.columns where TABLE_NAME='{0}' ORDER BY TABLE_NAME";
                return sql;
            }
        }
        protected override string GetTableInfoListSql
        {
            get
            {
                return @"select TABLE_NAME as Name,TABLE_COMMENT as Description from information_schema.tables
                         where  TABLE_SCHEMA=(select database())  AND TABLE_TYPE='BASE TABLE'";
            }
        }
        protected override string GetViewInfoListSql
        {
            get
            {
                return @"select TABLE_NAME as Name,TABLE_COMMENT as Description from information_schema.tables
                         where  TABLE_SCHEMA=(select database()) AND TABLE_TYPE='VIEW'
                         ";
            }
        }
        #endregion

        #region DDL
        protected override string AddPrimaryKeySql
        {
            get
            {
                return "ALTER TABLE {0} ADD CONSTRAINT {1} PRIMARY KEY({2})";
            }
        }
        protected override string AddColumnToTableSql
        {
            get
            {
                return "ALTER TABLE {0} ADD {1} {2}{3} {4} {5} {6}";
            }
        }
        protected override string AlterColumnToTableSql
        {
            get
            {
                return "ALTER TABLE {0} ALTER COLUMN {1} {2}{3} {4} {5} {6}";
            }
        }
        protected override string BackupDataBaseSql
        {
            get
            {
                return @"USE master;BACKUP DATABASE {0} TO disk = '{1}'";
            }
        }
        protected override string CreateTableSql
        {
            get
            {
                return "CREATE TABLE {0}(\r\n{1}, PRIMARY KEY (`{2}`))";
            }
        }
        protected override string CreateTableColumn
        {
            get
            {
                return "{0} {1}{2} {3} {4} {5}";
            }
        }
        protected override string TruncateTableSql
        {
            get
            {
                return "TRUNCATE TABLE {0}";
            }
        }
        protected override string BackupTableSql
        {
            get
            {
                return "SELECT TOP {0} *　INTO {1} FROM  {2}";
            }
        }
        protected override string DropTableSql
        {
            get
            {
                return "DROP TABLE {0}";
            }
        }
        protected override string DropColumnToTableSql
        {
            get
            {
                return "ALTER TABLE {0} DROP COLUMN {1}";
            }
        }
        protected override string DropConstraintSql
        {
            get
            {
                return "ALTER TABLE {0} DROP CONSTRAINT  {1}";
            }
        }
        protected override string RenameColumnSql
        {
            get
            {
                return "exec sp_rename '{0}.{1}','{2}','column';";
            }
        }
        #endregion

        #region Check
        protected override string CheckSystemTablePermissionsSql
        {
            get
            {
                return "select 1 from Information_schema.columns limit 0,1";
            }
        }
        #endregion

        #region Scattered
        protected override string CreateTableNull
        {
            get
            {
                return "DEFAULT NULL";
            }
        }
        protected override string CreateTableNotNull
        {
            get
            {
                return "NOT NULL";
            }
        }
        protected override string CreateTablePirmaryKey
        {
            get
            {
                return "PRIMARY KEY";
            }
        }
        protected override string CreateTableIdentity
        {
            get
            {
                return "AUTO_INCREMENT";
            }
        }
        #endregion

        #region Methods
        public override bool CreateTable(string tableName, List<DbColumnInfo> columns)
        {
            if (columns.IsValuable())
            {
                foreach (var item in columns)
                {
                    if (item.DbColumnName.Equals("GUID",StringComparison.CurrentCultureIgnoreCase))
                    {
                        item.Length = 10;
                    }
                }
            }
            return base.CreateTable(tableName,columns);
        }
        protected override string GetCreateTableSql(string tableName, List<DbColumnInfo> columns)
        {
            List<string> columnArray = new List<string>();
            Check.Exception(columns.IsNullOrEmpty(), "No columns found ");
            foreach (var item in columns)
            {
                string columnName = item.DbColumnName;
                string dataType = item.DataType;
                string dataSize = item.Length > 0 ? string.Format("({0})", item.Length) : null;
                string nullType = item.IsNullable ? this.CreateTableNull : CreateTableNotNull;
                string primaryKey = null;
                string identity = item.IsIdentity ? this.CreateTableIdentity : null;
                string addItem = string.Format(this.CreateTableColumn, this.SqlBuilder.GetTranslationColumnName(columnName), dataType, dataSize, nullType, primaryKey, identity);
                columnArray.Add(addItem);
            }
            string tableString = string.Format(this.CreateTableSql, this.SqlBuilder.GetTranslationTableName(tableName), string.Join(",\r\n", columnArray), columns.First(it=>it.IsPrimarykey).DbColumnName);
            return tableString;
        }
        #endregion
    }
}