using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi.Models;
using Tweetinvi.Models.DTO;
using Tweetinvi.Parameters;

namespace Examplinvi.DbFx.Models
{
    public static class DbListExtensions
    {
        public static DbList ToDbList(this ITwitterList list) => list.TwitterListDTO.ToDbList();
        public static DbList ToDbList(this ITwitterListDTO dto) => DbList.FromDto(dto);
    }
    public class DbList
    {
        [ExplicitKey]
        public long Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public long DbUserId { get; set; }
        //public virtual DbUser DbUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Uri { get; set; }
        public string Description { get; set; }
        public bool Following { get; set; }
        public int Public { get; set; }
        public int MemberCount { get; set; }
        public int SubscriberCount { get; set; }

        public static DbList FromDto(ITwitterListDTO dto)
        {
            var result = new DbList
            {
                Id = dto.Id,
                Name = dto.Name,
                FullName = dto.FullName,
                DbUserId = dto.OwnerId,
                //DbUser = dto.Owner.ToDbUser(),
                CreatedAt= dto.CreatedAt,
                Uri= dto.Uri,
                Description= dto.Description,
                Following= dto.Following,
                Public= (int)dto.PrivacyMode,
                MemberCount= dto.MemberCount,
                SubscriberCount = dto.MemberCount,
            }
            ;
            return result;
        }
    }
    public class DbListMember
    {
        public long Id { get; set; }
        public long DbListId { get; set; }
        public long DbUserId { get; set; }

    }
}
