using dai.core.Models;
using dai.core.Models.Entities;
using dai.core.Models.Notifications;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace dai.dataAccess.DbContext
{
    public class AppDbContext : IdentityDbContext<UserModel, UserRoleModel, Guid>
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer("Server=tcp:dainote.database.windows.net,1433;Initial Catalog=DaiNoteDB_v2;Persist Security Info=False;User ID=tuasan;Password=@Gonewinvn2002;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

        }
        public DbSet<UserModel> userModels { get; set; }
        public DbSet<NoteModel> Notes { get; set; }
        public DbSet<LabelModel> Labels { get; set; }
        public DbSet<NoteLabelModel> NoteLabels { get; set; }


        public DbSet<WorkspaceModel> Workspaces { get; set; }
        public DbSet<BoardModel> Boards { get; set; }
        public DbSet<TaskInListModel> TaskInList { get; set; }
        public DbSet<TaskModel> Tasks { get; set; }
        public DbSet<ListModel> lists { get; set; }


        public DbSet<CollaboratorModel> Collaborators { get; set; }
        public DbSet<CollaboratorInvitationModel> CollaboratorInvitations { get; set; }


        public DbSet<RevokedToken> RevokedTokens { get; set; }
        public DbSet<TransactionModel> Transactions { get; set; }


        public DbSet<HubConnection> HubConnections { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Chat> Chats { get; set; } = null!;
        public DbSet<ChatRoom> ChatRooms { get; set; } = null!;
        public DbSet<ChatRoomUser> ChatRoomUsers { get; set; } = null!;
        public DbSet<ChatPrivate> ChatPrivate { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName != null && tableName.StartsWith("AspNet"))
                {
                    entityType.SetTableName(tableName.Substring(6));
                }
            }


            ConfigureNoteAndLabel(modelBuilder);


            ConfigureCollaborators(modelBuilder);


            ConfigureTaskInList(modelBuilder);


            ConfigureTasks(modelBuilder);


            ConfigureTokens(modelBuilder);


            ConfigureTransactions(modelBuilder);
        }

        private void ConfigureNoteAndLabel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NoteModel>()
                .HasKey(n => n.Id);

            modelBuilder.Entity<NoteModel>()
                .HasOne(n => n.User)
                .WithMany(u => u.Note)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LabelModel>()
                .HasKey(l => l.Id);

            modelBuilder.Entity<LabelModel>()
                .HasOne(l => l.User)
                .WithMany(u => u.Label)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NoteLabelModel>()
                .HasKey(nl => new { nl.NoteId, nl.LabelId });

            modelBuilder.Entity<NoteLabelModel>()
                .HasOne(nl => nl.Note)
                .WithMany(n => n.NoteLabels)
                .HasForeignKey(nl => nl.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NoteLabelModel>()
                .HasOne(nl => nl.Label)
                .WithMany(l => l.NoteLabels)
                .HasForeignKey(nl => nl.LabelId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureCollaborators(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CollaboratorModel>()
                .HasKey(c => new { c.Board_Id, c.User_Id, c.Invitation_Code });

            modelBuilder.Entity<CollaboratorModel>()
                .HasOne(c => c.User)
                .WithMany(u => u.Collaborator)
                .HasForeignKey(c => c.User_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CollaboratorModel>()
                .HasOne(c => c.Board)
                .WithMany(b => b.Collaborators)
                .HasForeignKey(c => c.Board_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CollaboratorModel>()
                .HasOne(c => c.CollaboratorInvitation)
                .WithMany(i => i.Collaborators)
                .HasForeignKey(c => c.Invitation_Code)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CollaboratorInvitationModel>()
                .Property(c => c.Emails)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<ICollection<string>>(v)
                );
        }

        private void ConfigureTaskInList(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskInListModel>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<TaskInListModel>()
                .HasOne(t => t.Board)
                .WithMany(b => b.taskInList)
                .HasForeignKey(t => t.Board_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskInListModel>()
                .HasOne(t => t.List)
                .WithMany(l => l.taskInList)
                .HasForeignKey(t => t.List_Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TaskInListModel>()
                .HasOne(t => t.Task)
                .WithMany(task => task.taskInList)
                .HasForeignKey(t => t.Task_Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        }

        private void ConfigureTasks(ModelBuilder modelBuilder)
        {






            modelBuilder.Entity<TaskModel>()
                .Property(t => t.FileName)
                .IsRequired(false);
        }

        private void ConfigureTokens(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RevokedToken>()
                .Property(r => r.Token)
                .HasMaxLength(512);
        }
        private void ConfigureTransactions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransactionModel>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<TransactionModel>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TransactionModel>()
                .Property(t => t.Status)
                .HasMaxLength(50);

            modelBuilder.Entity<TransactionModel>()
                .Property(t => t.Description)
                .HasMaxLength(200);

            modelBuilder.Entity<TransactionModel>()
                .HasIndex(t => t.OrderCode)
                .IsUnique();

            modelBuilder.Entity<TransactionModel>()
        .HasOne(t => t.User)
        .WithMany(u => u.Transactions) // Navigation property ở UserModel
        .HasForeignKey(t => t.UserId)
        .OnDelete(DeleteBehavior.Restrict); // Tránh xóa chuỗi
        }
    }
}
