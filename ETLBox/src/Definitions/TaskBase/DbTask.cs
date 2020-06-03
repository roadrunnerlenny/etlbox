﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ETLBox.ControlFlow
{
    public abstract class DbTask : GenericTask
    {

        /* Public Properties */
        public string Sql { get; set; }
        public List<Action<object>> Actions { get; set; }
        public Action BeforeRowReadAction { get; set; }
        public Action AfterRowReadAction { get; set; }
        public long Limit { get; set; } = long.MaxValue;
        public int? RowsAffected { get; private set; }
        public bool IsOdbcConnection => DbConnectionManager.IsOdbcConnection;
        public virtual bool DoXMLCommentStyle { get; set; }
        public IDbTransaction Transaction { get; set; }
        internal virtual string NameAsComment => CommentStart + TaskName + CommentEnd + Environment.NewLine;
        private string CommentStart => DoXMLCommentStyle ? @"<!--" : "/*";
        private string CommentEnd => DoXMLCommentStyle ? @"-->" : "*/";
        public string Command
        {
            get
            {
                if (HasSql)
                    return HasName && !IsOdbcConnection ? NameAsComment + Sql : Sql;
                else
                    throw new Exception("Empty command");
            }
        }
        public IEnumerable<QueryParameter> Parameter { get; set; }

        /* Internal/Private properties */
        internal bool DoSkipSql { get; private set; }
        bool HasSql => !(String.IsNullOrWhiteSpace(Sql));

        /* Some constructors */
        public DbTask()
        {

        }

        public DbTask(string name) : this()
        {
            this.TaskName = name;
        }

        public DbTask(string name, string sql) : this(name)
        {
            this.Sql = sql;
        }

        public DbTask(ITask callingTask, string sql)
        {
            this.Sql = sql;
            CopyTaskProperties(callingTask);
        }

        public DbTask(string name, string sql, params Action<object>[] actions) : this(name, sql)
        {
            Actions = actions.ToList();
        }


        public DbTask(string name, string sql, Action beforeRowReadAction, Action afterRowReadAction, params Action<object>[] actions) : this(name, sql)
        {
            BeforeRowReadAction = beforeRowReadAction;
            AfterRowReadAction = afterRowReadAction;
            Actions = actions.ToList();
        }


        /* Public methods */
        public int ExecuteNonQuery()
        {
            var conn = DbConnectionManager.CloneIfAllowed();
            try
            {
                conn.Open();
                if (!DisableLogging) LoggingStart();
                RowsAffected = DoSkipSql ? 0 : conn.ExecuteNonQuery(Command, Parameter);
                if (!DisableLogging) LoggingEnd(LogType.Rows);
            }
            finally
            {
                conn.CloseIfAllowed();
            }
            return RowsAffected ?? 0;
        }

        public object ExecuteScalar()
        {
            object result = null;
            var conn = DbConnectionManager.CloneIfAllowed();
            try
            {
                conn.Open();
                if (!DisableLogging) LoggingStart();
                result = conn.ExecuteScalar(Command, Parameter);
                if (!DisableLogging) LoggingEnd();
            }
            finally
            {
                conn.CloseIfAllowed();
            }
            return result;
        }

        public Nullable<T> ExecuteScalar<T>() where T : struct
        {
            object result = ExecuteScalar();
            if (result == null || result == DBNull.Value)
                return null;
            else
                return (T)(Convert.ChangeType(result, typeof(T)));
        }


        public bool ExecuteScalarAsBool()
        {
            object result = ExecuteScalar();
            return ObjectToBool(result);
        }

        static bool ObjectToBool(object result)
        {
            if (result == null) return false;
            int number = 0;
            int.TryParse(result.ToString(), out number);
            if (number > 0)
                return true;
            else if (result.ToString().Trim().ToLower() == "true")
                return true;
            else
                return false;
        }

        public void ExecuteReader()
        {
            var conn = DbConnectionManager.CloneIfAllowed();
            try
            {
                conn.Open();
                if (!DisableLogging) LoggingStart();
                using (IDataReader reader = conn.ExecuteReader(Command, Parameter) as IDataReader)
                {
                    for (int rowNr = 0; rowNr < Limit; rowNr++)
                    {
                        if (reader.Read())
                        {
                            BeforeRowReadAction?.Invoke();
                            for (int i = 0; i < Actions?.Count; i++)
                            {
                                if (!reader.IsDBNull(i))
                                {
                                    Actions?[i]?.Invoke(reader.GetValue(i));
                                }
                                else
                                {
                                    Actions?[i]?.Invoke(null);
                                }
                            }
                            AfterRowReadAction?.Invoke();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (!DisableLogging) LoggingEnd();
            }
            finally
            {
                conn.CloseIfAllowed();
            }
        }

        public void BulkInsert(ITableData data, string tableName)
        {
            var conn = DbConnectionManager.CloneIfAllowed();
            try
            {
                conn.Open();
                if (!DisableLogging) LoggingStart(LogType.Bulk);
                conn.BeforeBulkInsert(tableName);
                conn.BulkInsert(data, tableName);
                conn.AfterBulkInsert(tableName);
                RowsAffected = data.RecordsAffected;
                if (!DisableLogging) LoggingEnd(LogType.Bulk);
            }
            finally
            {
                conn.CloseIfAllowed();
            }
        }


        /* Private implementation & stuff */
        enum LogType
        {
            None,
            Rows,
            Bulk
        }


        void LoggingStart(LogType logType = LogType.None)
        {
            NLogger.Info(TaskName, TaskType, "START", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
            if (logType == LogType.Bulk)
                NLogger.Debug($"SQL Bulk Insert", TaskType, "RUN", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
            else
                NLogger.Debug($"{Command}", TaskType, "RUN", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
        }

        void LoggingEnd(LogType logType = LogType.None)
        {
            NLogger.Info(TaskName, TaskType, "END", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
            if (logType == LogType.Rows)
                NLogger.Debug($"Rows affected: {RowsAffected ?? 0}", TaskType, "RUN", TaskHash, ControlFlow.STAGE, ControlFlow.CurrentLoadProcess?.Id);
        }
    }
}
