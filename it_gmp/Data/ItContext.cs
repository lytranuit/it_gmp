using it.Areas.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using System.Data.Common;
using Microsoft.Extensions.DiagnosticAdapter;
namespace it.Data
{
    public class ItContext : DbContext
    {
        private IActionContextAccessor actionAccessor;
        private UserManager<UserModel> UserManager;
        public ItContext(DbContextOptions<ItContext> options, UserManager<UserModel> UserMgr, IActionContextAccessor ActionAccessor) : base(options)
        {
            actionAccessor = ActionAccessor;
            UserManager = UserMgr;
        }

        public DbSet<AuditTrailsModel> AuditTrailsModel { get; set; }

        public DbSet<UserModel> UserModel { get; set; }
        public DbSet<UserRoleModel> UserRoleModel { get; set; }
		public DbSet<UserDocumentTypeModel> UserDocumentTypeModel { get; set; }
		public DbSet<TemplateModel> TemplateModel { get; set; }
        //public DbSet<User2Model> User2Model { get; set; }
        public DbSet<EmailModel> EmailModel { get; set; }
        public DbSet<DocumentTypeModel> DocumentTypeModel { get; set; }
        public DbSet<DocumentTypeGroupModel> DocumentTypeGroupModel { get; set; }
        public DbSet<DocumentTypeReceiveModel> DocumentTypeReceiveModel { get; set; }
        public DbSet<DocumentModel> DocumentModel { get; set; }
        public DbSet<DocumentFileModel> DocumentFileModel { get; set; }

        public DbSet<DocumentAttachmentModel> DocumentAttachmentModel { get; set; }
        public DbSet<DocumentRelatedModel> DocumentRelatedModel { get; set; }
        public DbSet<DocumentSignatureModel> DocumentSignatureModel { get; set; }
        public DbSet<DocumentUserReceiveModel> DocumentUserReceiveModel { get; set; }
        public DbSet<DocumentCommentModel> DocumentCommentModel { get; set; }
        public DbSet<DocumentCommentFileModel> DocumentCommentFileModel { get; set; }
        public DbSet<DocumentUserReadModel> DocumentUserReadModel { get; set; }
        public DbSet<DocumentUserKeywordModel> DocumentUserKeywordModel { get; set; }
        public DbSet<DocumentUserUnreadModel> DocumentUserUnreadModel { get; set; }
        public DbSet<DocumentEventModel> DocumentEventModel { get; set; }
        public DbSet<TokenModel> TokenModel { get; set; }

        public DbSet<Chart> Chart { get; set; }
        public DbSet<ChartPie> ChartPie { get; set; }
        public DbSet<ChartType> ChartType { get; set; }
        public DbSet<ChartTypeGroup> ChartTypeGroup { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<IdentityUser>().ToTable("AspNetUsers");

            //modelBuilder.Entity<DocumentModel>().HasMany(l => l.Teams).WithOne().HasForeignKey("LeagueId");
            //modelBuilder.Entity<ProcessTableDataModel>()
            // .Property(b => b._data).HasColumnName("data");
            modelBuilder.Entity<UserRoleModel>().ToTable("AspNetUserRoles").HasKey(table => new
            {
                table.RoleId,
                table.UserId
            });

        }
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
        }
        public override int SaveChanges()
        {
            OnBeforeSaveChanges();
            return base.SaveChanges();
        }
        public int Save()
        {
            return base.SaveChanges();
        }
        private void OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var user_http = actionAccessor.ActionContext.HttpContext.User;
            var user_id = UserManager.GetUserId(user_http);
            var changes = ChangeTracker.Entries();
            foreach (var entry in changes)
            {
                if (entry.Entity is EmailModel || entry.Entity is TokenModel)
                    continue;

                if (entry.Entity is AuditTrailsModel || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name;
                auditEntry.UserId = user_id;
                auditEntries.Add(auditEntry);
                foreach (var property in entry.Properties)
                {

                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.AuditType = AuditType.Create;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;
                        case EntityState.Deleted:
                            auditEntry.AuditType = AuditType.Delete;
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;
                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                var Original = entry.GetDatabaseValues().GetValue<object>(propertyName);
                                var Current = property.CurrentValue;
                                if (JsonConvert.SerializeObject(Original) == JsonConvert.SerializeObject(Current))
                                    continue;
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.AuditType = AuditType.Update;
                                auditEntry.OldValues[propertyName] = Original;
                                auditEntry.NewValues[propertyName] = Current;

                            }
                            break;
                    }

                }
            }
            foreach (var auditEntry in auditEntries)
            {
                AuditTrailsModel.Add(auditEntry.ToAudit());
            }
        }
    }
    public class CommandInterceptor
    {
        [DiagnosticName("Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting")]
        public void OnCommandExecuting(DbCommand command, DbCommandMethod executeMethod, Guid commandId, Guid connectionId, bool async, DateTimeOffset startTime)
        {
            var secondaryDatabaseName = "OrgData";
            var schemaName = "dbo";
            var list_talbe = new List<string>()
            {
                "AspNetUsers","AspNetUserRoles","emails","Token"
            };
            //var tableName = "AspNetUsers";
            foreach (var tableName in list_talbe)
            {
                command.CommandText = command.CommandText.Replace($" [{tableName}]", $" [{schemaName}].[{tableName}]")
                                                     .Replace($" [{schemaName}].[{tableName}]", $" [{secondaryDatabaseName}].[{schemaName}].[{tableName}]");
            }


        }
    }
}
