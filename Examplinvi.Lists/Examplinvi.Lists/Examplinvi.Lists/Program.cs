using Dapper;
using Dapper.Contrib.Extensions;
using Examplinvi.DbFx.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;

namespace Examplinvi.Lists
{
    class Program
    {
        static string connString = "server=.;initial catalog=tdb;trusted_connection=true;";
        static Program() { Creds.Helper.SetCreds(); }
        static void Main(string[] args)
        {
            CreateSenateAndHouseLists();
        }


        static void CreateSenateAndHouseLists()
        {
            using (var repo = new DbFx.DbRepo())
            {

                using (var conn = new SqlConnection(connString))
                {
                    var users = conn.Query<DbUser>(@"select u.* from DbListMembers m join dbUsers u
                                on m.DbUserId=u.Id
                                where m.DbListId=1187394609611194370").ToList();
                    var senators = conn.Query<DbUser>(@"
select u.* from DbListMembers m join dbUsers u
on m.DbUserId=u.Id
where m.DbListId=1187394609611194370
and (
screenname like 'sen%' or ScreenName like 'Senator%' or  name like 'sen%' or name like 'Senator%'
or description like '%US Senat%' or description like '%U.S. Senat%' or description like '%United States Senat%'

)").ToList();
                    var senatorIds = senators.Select(x => x.Id).ToList();
                    var house = users.Where(x => !senatorIds.Contains(x.Id)).ToList();
                    var houseIds = house.Select(x => x.Id).ToList();

                  
                    var senateListName = "US Senate Members";

                    var senateList = ListHelper.Create(senateListName, senateListName);
                    senateList.AddMultipleMembers(senatorIds);


                    var houseListName = "US House Members";
                    var houseList= ListHelper.Create(houseListName, houseListName);
                    houseList.AddMultipleMembers(houseIds);
                    //houseList.Destroy();
                    //senateList.Destroy();


                }

            }


        }

        static void AssureListUsers()
        {
            using (var repo = new DbFx.DbRepo())
            {
                var count = repo.Context.Users.Count();
            }
            var user = User.GetAuthenticatedUser();
            var slug = "members-of-congress";
            var list = ListHelper.Get(slug, user.Id);
            var dbList = list.ToDbList();
            var members = list.GetMembers(int.MaxValue);

            using (var repo = new DbFx.DbRepo())
            {

                using (var conn = new SqlConnection(connString))
                {
                    var myDbUser = user.ToDbUser();
                    var myExistingUser = conn.Get<DbUser>(myDbUser.Id);
                    if (myExistingUser == null)
                    {
                        repo.Add(myDbUser);
                    }
                    else
                    {
                        repo.Update(myDbUser);
                    }
                    var existingList = conn.Get<DbList>(dbList.Id);
                    if (existingList == null)
                    {
                        conn.Insert(dbList);
                    }
                    else
                    {
                        conn.Update(dbList);
                    }
                    var DbListId = list.Id;
                    var dbListMembers = conn.Query<DbListMember>("select * from DbListMembers where  DbListId=@DbListId", new { DbListId }).ToList();
                    foreach (var member in members)
                    {
                        var dbUser = member.ToDbUser();
                        var existingListMember = dbListMembers.FirstOrDefault(x => x.DbUserId == dbUser.Id); // dbMembers.IndexOf(dbUser. conn.Get<DbUser>(dbUser.Id);
                        if (existingListMember == null)
                        {
                            // if the member isn't in the db list make sure the dbUser exists
                            var existingDbUser = conn.Get<DbUser>(dbUser.Id);
                            if (existingDbUser == null)
                            {
                                repo.Add(dbUser);
                            }
                            else
                            {
                                string bp = "";
                            }
                        }
                        var memberUpsertQuery = @"declare @id int=IsNull((select top 1 id from DbListMembers where DbListId=@DbListId and DbUserId=@DbUserId), 0)
if (@id = 0)
begin
	insert into DbListMembers(DbListId, DbUserId) values(@DbListId, @DbUserId)
	set @id = @@IDENTITY
end
select @id";
                        var dbListMember = conn.Execute(memberUpsertQuery, new { DbListId, DbUserId = member.Id });
                    }

                }
            }

        }
        private static void CopyCRList()
        {

            var user = User.GetUserFromScreenName("govtrack");
            var slug = "members-of-congress";
            var list = ListHelper.Get(slug, user.Id);
            var dbList = list.ToDbList();
            ListHelper.Copy(list);
        }
    }

    public class ListHelper
    {
        public static ITwitterList Create(string name, string description, bool isPrivate = false)
        {
            PrivacyMode privacy = isPrivate ? PrivacyMode.Private : PrivacyMode.Public;
            var list = TwitterList.CreateList($"{name}", PrivacyMode.Public, $"{description}");
            return list;
        }
        public static ITwitterList Get(long list_id)
            => TwitterList.GetExistingList(list_id);
        public static ITwitterList Get(ITwitterListIdentifier twitterListIdentifier)
            => TwitterList.GetExistingList(twitterListIdentifier);
        public static ITwitterList Get(string slug, IUserIdentifier userId)
            => TwitterList.GetExistingList(slug, userId);
        public static ITwitterList Get(string slug, long userId)
            => TwitterList.GetExistingList(slug, userId);
        public static ITwitterList Get(string slug, string userScreenName)
            => TwitterList.GetExistingList(slug, userScreenName);

        public static ITwitterList Copy(ITwitterList list, bool isPrivate = false)
        {
            var name = list.Name;
            var description = list.Description;
            var dest = Create(name, description, isPrivate);
            var members = list.GetMembers(int.MaxValue);
            var memberIds = members.OrderBy(x => x.ScreenName).Select(x => x.UserIdentifier).ToList();
            dest.AddMultipleMembers(memberIds);
            var json = members.ToJson();
            //dest.Destroy();

            return dest;
        }
    }
}
