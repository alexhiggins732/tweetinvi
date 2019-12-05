using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Data;
using System.Data.SqlClient;
namespace Examplinvi.DbEditor
{
    public class SenateMemberLoader
    {
        public static void Load(Form1 form1)
        {
            var doc = new XmlDocument();
            doc.Load("https://www.senate.gov/general/contact_information/senators_cfm.xml");
            var rdr = XmlReader.Create("https://www.senate.gov/general/contact_information/senators_cfm.xml");

            var ser = new XmlSerializer(typeof(contact_information));

            var memberData = (contact_information)ser.Deserialize(rdr);
            using (var conn = new SqlConnection(form1.connectionstring()))
            {
                var items = memberData.member.ToList();
                conn.ImportDataList(items, "SenateMembers_20191025");
            }
        }
    }

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
        [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class contact_information
    {

        private contact_informationMember[] memberField;

        private string last_updatedField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("member")]
        public contact_informationMember[] member
        {
            get
            {
                return this.memberField;
            }
            set
            {
                this.memberField = value;
            }
        }

        /// <remarks/>
        public string last_updated
        {
            get
            {
                return this.last_updatedField;
            }
            set
            {
                this.last_updatedField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class contact_informationMember
    {

        private string member_fullField;

        private string last_nameField;

        private string first_nameField;

        private string partyField;

        private string stateField;

        private string addressField;

        private string phoneField;

        private string emailField;

        private string websiteField;

        private string classField;

        private string bioguide_idField;

        private string leadership_positionField;

        /// <remarks/>
        public string member_full
        {
            get
            {
                return this.member_fullField;
            }
            set
            {
                this.member_fullField = value;
            }
        }

        /// <remarks/>
        public string last_name
        {
            get
            {
                return this.last_nameField;
            }
            set
            {
                this.last_nameField = value;
            }
        }

        /// <remarks/>
        public string first_name
        {
            get
            {
                return this.first_nameField;
            }
            set
            {
                this.first_nameField = value;
            }
        }

        /// <remarks/>
        public string party
        {
            get
            {
                return this.partyField;
            }
            set
            {
                this.partyField = value;
            }
        }

        /// <remarks/>
        public string state
        {
            get
            {
                return this.stateField;
            }
            set
            {
                this.stateField = value;
            }
        }

        /// <remarks/>
        public string address
        {
            get
            {
                return this.addressField;
            }
            set
            {
                this.addressField = value;
            }
        }

        /// <remarks/>
        public string phone
        {
            get
            {
                return this.phoneField;
            }
            set
            {
                this.phoneField = value;
            }
        }

        /// <remarks/>
        public string email
        {
            get
            {
                return this.emailField;
            }
            set
            {
                this.emailField = value;
            }
        }

        /// <remarks/>
        public string website
        {
            get
            {
                return this.websiteField;
            }
            set
            {
                this.websiteField = value;
            }
        }

        /// <remarks/>
        public string @class
        {
            get
            {
                return this.classField;
            }
            set
            {
                this.classField = value;
            }
        }

        /// <remarks/>
        public string bioguide_id
        {
            get
            {
                return this.bioguide_idField;
            }
            set
            {
                this.bioguide_idField = value;
            }
        }

        /// <remarks/>
        public string leadership_position
        {
            get
            {
                return this.leadership_positionField;
            }
            set
            {
                this.leadership_positionField = value;
            }
        }
    }


}
