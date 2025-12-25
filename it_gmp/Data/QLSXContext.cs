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
    public class QLSXContext : DbContext
    {
        private IActionContextAccessor actionAccessor;
        private UserManager<UserModel> UserManager;
        public QLSXContext(DbContextOptions<QLSXContext> options, UserManager<UserModel> UserMgr, IActionContextAccessor ActionAccessor) : base(options)
        {
            actionAccessor = ActionAccessor;
            UserManager = UserMgr;
        }


        public DbSet<TBL_DANHMUCKHUVUC> TBL_DANHMUCKHUVUC { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           

        }
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
        }
       
    }
    
}
