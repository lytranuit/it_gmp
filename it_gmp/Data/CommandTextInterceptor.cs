using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace it.Data
{
    /// <summary>
    /// EF Core Interceptor thay thế CommandInterceptor cũ (DiagnosticListener).
    /// Đăng ký 1 lần duy nhất trong Program.cs, tránh memory leak.
    /// Rewrite SQL để trỏ một số bảng sang database OrgData.
    /// </summary>
    public class CommandTextInterceptor : DbCommandInterceptor
    {
        private static readonly List<string> _tables = new()
        {
            "AspNetUsers", "AspNetUserRoles", "emails", "Token"
        };
        private const string SecondaryDb = "OrgData";
        private const string Schema = "dbo";

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            RewriteCommand(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            RewriteCommand(command);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            RewriteCommand(command);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            RewriteCommand(command);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            RewriteCommand(command);
            return result;
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            RewriteCommand(command);
            return ValueTask.FromResult(result);
        }

        private static void RewriteCommand(DbCommand command)
        {
            foreach (var tableName in _tables)
            {
                command.CommandText = command.CommandText
                    .Replace($" [{tableName}]", $" [{Schema}].[{tableName}]")
                    .Replace($" [{Schema}].[{tableName}]", $" [{SecondaryDb}].[{Schema}].[{tableName}]");
            }
        }
    }
}
