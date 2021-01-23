using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Logic.DTO;
using Tweetinvi.Models;
using Examplinvi.DbFx.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Examplinvi.DbFx
{

    public class TDbContext : DbContext
    {
        public static void Test(string[] args)
        {

            using (var ctx = new TDbContext())
            {
                var users = ctx.Users.ToList();
            }

            List<IUser> friends = null;
            friends = JsonSerializer.ConvertJsonTo<List<IUser>>(File.ReadAllText($"{nameof(friends)}.json"));
            List<IUser> followers = null;
            var friendIds = friends.Select(x => x.Id);

            followers = JsonSerializer.ConvertJsonTo<List<IUser>>(File.ReadAllText($"{nameof(followers)}.json")).Where(x => !friendIds.Contains(x.Id)).ToList();
            var followerIds = followers.Select(x => x.Id);

            List<DbUser> dbUsers = new List<DbUser>();
            dbUsers.Add(User.GetAuthenticatedUser().ToDbUser());

            var dtos = friends.Select(x => x.UserDTO.ToDbUser(friendIds.Contains(x.Id))).ToList();
            dtos.ForEach(user => user.WhiteListed = true);
            dbUsers.AddRange(dtos);

            dtos.AddRange(followers.Select(x => x.UserDTO.ToDbUser(true)));


            using (var ctx = new TDbContext())
            {
                //var stud = new UserDTO() {};
                ctx.Users.AddRange(dtos);
                //ctx.UserDTOs.Add(stud);
                ctx.SaveChanges();
            }
        }
        public TDbContext() : base("name=TDBConnectionString")
        {

            Database.SetInitializer<TDbContext>(new CreateDatabaseIfNotExists<TDbContext>());

            //Database.SetInitializer<SchoolDBContext>(new DropCreateDatabaseIfModelChanges<SchoolDBContext>());
            //Database.SetInitializer<SchoolDBContext>(new DropCreateDatabaseAlways<SchoolDBContext>());
            //Database.SetInitializer<SchoolDBContext>(new SchoolDBInitializer());
        }

        public DbSet<DbUser> Users { get; set; }
        public DbSet<DbTweet> Tweets { get; set; }
        public DbSet<DbTweetMedia> Media { get; set; }
        public DbSet<DbVideoDetails> Videoes { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            ConfigureUserModel(modelBuilder);
            ConfigureTweetModel(modelBuilder);
            ConfigureMediaModel(modelBuilder);
            ConfigureVideoModel(modelBuilder);



            base.OnModelCreating(modelBuilder);
        }

        private void ConfigureVideoModel(DbModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DbVideoDetails>();

            entity.Property(a => a.Id)
               .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            entity.Property(a => a.ContentType)
              .HasMaxLength(100);
            entity.Property(a => a.URL)
                .HasMaxLength(1000);



        }
        private void ConfigureMediaModel(DbModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DbTweetMedia>();

            entity.Property(a => a.Id)
               .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            entity.Property(a => a.MediaType)
              .HasMaxLength(100);
            entity.Property(a => a.DisplayURL)
                .HasMaxLength(1000);
            entity.Property(a => a.ExpandedURL)
               .HasMaxLength(1000);
            entity.Property(a => a.MediaURL)
                .HasMaxLength(1000);
            entity.Property(a => a.MediaURLHttps)
             .HasMaxLength(1000);
            entity.HasMany(x => x.VideoDetails);

        }
        private void ConfigureTweetModel(DbModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DbTweet>();

            entity.Property(a => a.Id)
               .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            entity.Property(a => a.Text)
                .HasMaxLength(300);
        }

        private void ConfigureUserModel(DbModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DbUser>();

            entity.Property(a => a.Id)
               .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            entity.Property(a => a.Name)
                .HasMaxLength(100);
            entity.Property(a => a.ScreenName)
                .HasMaxLength(100);
            //entity.Property(a => a.Url)
            //    .HasMaxLength(100);
            entity.Property(a => a.Description)
             .HasMaxLength(260);
        }


    }

    public class TDBInitializer : CreateDatabaseIfNotExists<TDbContext>
    {
        protected override void Seed(TDbContext context)
        {
            base.Seed(context);
        }
    }

    public class DbRepo : IDisposable
    {
        public TDbContext Context;

        public DbRepo()
        {
            this.Context = new TDbContext();
        }
        public List<DbUser> UsersFollowingMe()
        {
            var users = Context.Users.Where(x => x.FollowsMe == true).ToList();
            return users;
        }

        public List<DbUser> UsersIFollow()
        {
            var users = Context.Users.Where(x => x.Following == true).ToList();
            return users;
        }


        public List<DbUser> UsersIFollowThatDontFollowMe(bool whitelisted)
        {
            var users = Context.Users.Where(x => x.Following == true && x.FollowsMe == false && x.WhiteListed == whitelisted).ToList();
            return users;
        }

        public void Add<T>(Func<T> entry) where T : class
            => Add(entry);

        public void Add<T>(T entry) where T : class
        {
            Context.Set<T>().Add(entry);
            Context.SaveChanges();
        }
        public void Add<T>(IEnumerable<T> entries) where T : class
        {
            Context.Set<T>().AddRange(entries);
            Context.SaveChanges();
        }
        public void Add<T>(Func<IEnumerable<T>> p) where T : class
            => Add(p());


        public void Update<T>(Func<IEnumerable<T>> entries) where T : class
            => Update(entries());
        public void Update<T>(IEnumerable<T> entries) where T : class
        {
            entries.ToList().ForEach(x => Update(x));
        }
        public void Update<T>(T entry) where T : class
        {

            Context.Set<T>().Attach(entry);
            Context.Entry(entry).State = EntityState.Modified;
            int updated = Context.SaveChanges();

        }

        public TEntity GetById<TEntity>(long id) where TEntity : class, IDbEntity
        {
            return Context.Set<TEntity>().FirstOrDefault(x => x.Id == id);
        }

        public void Update<T>(Func<T> p) where T : class
            => Update(p());

        public DbUser GetUserById(long id)
        {
            return GetById<DbUser>(id);
        }

        public void Dispose()
        {
            ((IDisposable)Context).Dispose();
        }
    }
}
