using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Examplinvi.DbEditor
{

    public static class BulkCopyExtensions
    {
        public static string ImportDataList<T>(this SqlConnection conn, List<T> list, string tableName = null)
        {
            var actions = new List<Action<T, DataRow>>();
            var dt = new DataTable();
            var inf = typeof(T);
            foreach (var prop in inf.GetProperties())
            {
                dt.Columns.Add(prop.Name, prop.PropertyType);
                actions.Add((info, row) =>
                {
                    row[prop.Name] = prop.GetValue(info);
                });
            }
            foreach (var item in list)
            {
                var row = dt.NewRow();
                actions.ForEach(action => action(item, row));
                dt.Rows.Add(row);
            }
            if (!string.IsNullOrEmpty(tableName))
            {
                dt.TableName = tableName;
            }
            return conn.ImportDataTable(dt);
        }
        public static string ImportDataTable(this SqlConnection conn, DataTable table)
        {
            conn.Open();
            if (table.TableName == "")
            {
                table.TableName = $"zzzz_Import_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}";
            }
            var existsQuery = "select count(0) from sys.tables t where name=@TableName";
            bool exists = conn.QueryFirst<bool>(existsQuery, new { table.TableName });
            if (!exists)
            {
                conn.CreateTable(table);
            }
            using (var copy = new SqlBulkCopy(conn))
            {
                copy.DestinationTableName = table.TableName;
                copy.WriteToServer(table);
            }
            return table.TableName;
        }
        public static void CreateTable(this SqlConnection conn, DataTable table)
        {

            var columns = table.Columns.Cast<DataColumn>().ToList();
            var defs = columns.Select(column => column.SqlColumnDefinition()).ToList();
            var script = $"Create Table [{table.TableName}] ({string.Join(", ", defs)})";
            conn.Execute(script);

        }
        public static string SqlColumnDefinition(this DataColumn column)
        {

            var type = column.DataType;
            var typeName = type.Name;
            if (typeName == typeof(object).Name)
                typeName = GetTypeNameFromData(column);
            switch (typeName)
            {
                case "String":
                    return $"{column.ColumnName} varchar(max)";
                case "int32":
                    return $"{column.ColumnName} int";
                default:
                    throw new Exception("Invalid Data Type: {typeName}");
            }


        }

        private static string GetTypeNameFromData(DataColumn column)
        {
            return "string";
        }
    }
    public class HouseMemberLoader
    {
        public static void Load(Form1 form1)
        {
            var doc = new XmlDocument();
            doc.Load("http://clerk.house.gov/xml/lists/MemberData.xml");
            var rdr = XmlReader.Create("http://clerk.house.gov/xml/lists/MemberData.xml");

            var ser = new XmlSerializer(typeof(MemberData));

            var memberData = (MemberData)ser.Deserialize(rdr);

            var len = memberData.members.Length;
            var dt = new DataTable();

            dt.Columns.Add("statedistrict");
            var actions = new List<Action<MemberDataMember, DataRow>>();
            actions.Add((info, row) => row["statedistrict"] = info.statedistrict);

            var inf = typeof(MemberDataMemberMemberinfo);
            foreach (var prop in inf.GetProperties())
            {

                if (prop.Name == nameof(MemberDataMemberMemberinfo.state))
                {
                    dt.Columns.Add(nameof(MemberDataMemberMemberinfoState.statefullname));
                    dt.Columns.Add(nameof(MemberDataMemberMemberinfoState.postalcode));
                    actions.Add((memberDataMember, row) =>
                    {
                        var info = memberDataMember.memberinfo;
                        var state = (MemberDataMemberMemberinfoState)prop.GetValue(info);
                        row[nameof(MemberDataMemberMemberinfoState.statefullname)] = state.statefullname;
                        row[nameof(MemberDataMemberMemberinfoState.postalcode)] = state.postalcode;
                    });
                }
                else if (prop.Name == nameof(MemberDataMemberMemberinfo.electeddate))
                {
                    dt.Columns.Add(nameof(MemberDataMemberMemberinfo.electeddate));

                    actions.Add((memberDataMember, row) =>
                    {
                        var info = memberDataMember.memberinfo;
                        row[nameof(MemberDataMemberMemberinfo.electeddate)] = info.electeddate.Value != null ? (DateTime?)DateTime.Parse(info.electeddate.Value) : null;

                    });
                }
                else if (prop.Name == nameof(MemberDataMemberMemberinfo.sworndate))
                {
                    dt.Columns.Add(nameof(MemberDataMemberMemberinfo.sworndate));
                    actions.Add((memberDataMember, row) =>
                    {
                        var info = memberDataMember.memberinfo;
                        row[nameof(MemberDataMemberMemberinfo.sworndate)] = info.sworndate.Value != null ? (DateTime?)DateTime.Parse(info.sworndate.Value) : null;

                    });
                }
                else
                {
                    dt.Columns.Add(prop.Name);
                    actions.Add((memberDataMember, row) =>
                    {
                        var info = memberDataMember.memberinfo;
                        row[prop.Name] = prop.GetValue(info);
                    });
                }

            }
            foreach (var member in memberData.members)
            {
                var row = dt.NewRow();
                actions.ForEach(action => action(member, row));
                dt.Rows.Add(row);

            }
            //dt.TableName = "HouseMembers";
            using (var conn = new SqlConnection(form1.connectionstring()))
            {
                conn.ImportDataTable(dt);
            }

            var bk = "Todo: save to database";
        }
    }

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class MemberData
    {

        private MemberDataTitleinfo titleinfoField;

        private MemberDataMember[] membersField;

        private MemberDataCommittee[] committeesField;

        private string publishdateField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("title-info")]
        public MemberDataTitleinfo titleinfo
        {
            get
            {
                return this.titleinfoField;
            }
            set
            {
                this.titleinfoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("member", IsNullable = false)]
        public MemberDataMember[] members
        {
            get
            {
                return this.membersField;
            }
            set
            {
                this.membersField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("committee", IsNullable = false)]
        public MemberDataCommittee[] committees
        {
            get
            {
                return this.committeesField;
            }
            set
            {
                this.committeesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("publish-date")]
        public string publishdate
        {
            get
            {
                return this.publishdateField;
            }
            set
            {
                this.publishdateField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataTitleinfo
    {

        private byte congressnumField;

        private string congresstextField;

        private byte sessionField;

        private string majorityField;

        private string minorityField;

        private string clerkField;

        private string weburlField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("congress-num")]
        public byte congressnum
        {
            get
            {
                return this.congressnumField;
            }
            set
            {
                this.congressnumField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("congress-text")]
        public string congresstext
        {
            get
            {
                return this.congresstextField;
            }
            set
            {
                this.congresstextField = value;
            }
        }

        /// <remarks/>
        public byte session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }

        /// <remarks/>
        public string majority
        {
            get
            {
                return this.majorityField;
            }
            set
            {
                this.majorityField = value;
            }
        }

        /// <remarks/>
        public string minority
        {
            get
            {
                return this.minorityField;
            }
            set
            {
                this.minorityField = value;
            }
        }

        /// <remarks/>
        public string clerk
        {
            get
            {
                return this.clerkField;
            }
            set
            {
                this.clerkField = value;
            }
        }

        /// <remarks/>
        public string weburl
        {
            get
            {
                return this.weburlField;
            }
            set
            {
                this.weburlField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMember
    {

        private string statedistrictField;

        private MemberDataMemberMemberinfo memberinfoField;

        private MemberDataMemberPredecessorinfo predecessorinfoField;

        private MemberDataMemberCommitteeassignments committeeassignmentsField;

        /// <remarks/>
        public string statedistrict
        {
            get
            {
                return this.statedistrictField;
            }
            set
            {
                this.statedistrictField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("member-info")]
        public MemberDataMemberMemberinfo memberinfo
        {
            get
            {
                return this.memberinfoField;
            }
            set
            {
                this.memberinfoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("predecessor-info")]
        public MemberDataMemberPredecessorinfo predecessorinfo
        {
            get
            {
                return this.predecessorinfoField;
            }
            set
            {
                this.predecessorinfoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("committee-assignments")]
        public MemberDataMemberCommitteeassignments committeeassignments
        {
            get
            {
                return this.committeeassignmentsField;
            }
            set
            {
                this.committeeassignmentsField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMemberMemberinfo
    {

        private string namelistField;

        private string bioguideIDField;

        private string lastnameField;

        private string firstnameField;

        private string middlenameField;

        private string sortnameField;

        private string suffixField;

        private string courtesyField;

        private byte priorcongressField;

        private string officialnameField;

        private string formalnameField;

        private string partyField;

        private string caucusField;

        private MemberDataMemberMemberinfoState stateField;

        private string districtField;

        private string townnameField;

        private string officebuildingField;

        private ushort officeroomField;

        private ushort officezipField;

        private ushort officezipsuffixField;

        private string phoneField;

        private MemberDataMemberMemberinfoElecteddate electeddateField;

        private MemberDataMemberMemberinfoSworndate sworndateField;

        private byte footnoterefField;

        private bool footnoterefFieldSpecified;

        private string footnoteField;

        /// <remarks/>
        public string namelist
        {
            get
            {
                return this.namelistField;
            }
            set
            {
                this.namelistField = value;
            }
        }

        /// <remarks/>
        public string bioguideID
        {
            get
            {
                return this.bioguideIDField;
            }
            set
            {
                this.bioguideIDField = value;
            }
        }

        /// <remarks/>
        public string lastname
        {
            get
            {
                return this.lastnameField;
            }
            set
            {
                this.lastnameField = value;
            }
        }

        /// <remarks/>
        public string firstname
        {
            get
            {
                return this.firstnameField;
            }
            set
            {
                this.firstnameField = value;
            }
        }

        /// <remarks/>
        public string middlename
        {
            get
            {
                return this.middlenameField;
            }
            set
            {
                this.middlenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("sort-name")]
        public string sortname
        {
            get
            {
                return this.sortnameField;
            }
            set
            {
                this.sortnameField = value;
            }
        }

        /// <remarks/>
        public string suffix
        {
            get
            {
                return this.suffixField;
            }
            set
            {
                this.suffixField = value;
            }
        }

        /// <remarks/>
        public string courtesy
        {
            get
            {
                return this.courtesyField;
            }
            set
            {
                this.courtesyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("prior-congress")]
        public byte priorcongress
        {
            get
            {
                return this.priorcongressField;
            }
            set
            {
                this.priorcongressField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("official-name")]
        public string officialname
        {
            get
            {
                return this.officialnameField;
            }
            set
            {
                this.officialnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("formal-name")]
        public string formalname
        {
            get
            {
                return this.formalnameField;
            }
            set
            {
                this.formalnameField = value;
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
        public string caucus
        {
            get
            {
                return this.caucusField;
            }
            set
            {
                this.caucusField = value;
            }
        }

        /// <remarks/>
        public MemberDataMemberMemberinfoState state
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
        public string district
        {
            get
            {
                return this.districtField;
            }
            set
            {
                this.districtField = value;
            }
        }

        /// <remarks/>
        public string townname
        {
            get
            {
                return this.townnameField;
            }
            set
            {
                this.townnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("office-building")]
        public string officebuilding
        {
            get
            {
                return this.officebuildingField;
            }
            set
            {
                this.officebuildingField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("office-room")]
        public ushort officeroom
        {
            get
            {
                return this.officeroomField;
            }
            set
            {
                this.officeroomField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("office-zip")]
        public ushort officezip
        {
            get
            {
                return this.officezipField;
            }
            set
            {
                this.officezipField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("office-zip-suffix")]
        public ushort officezipsuffix
        {
            get
            {
                return this.officezipsuffixField;
            }
            set
            {
                this.officezipsuffixField = value;
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
        [System.Xml.Serialization.XmlElementAttribute("elected-date")]
        public MemberDataMemberMemberinfoElecteddate electeddate
        {
            get
            {
                return this.electeddateField;
            }
            set
            {
                this.electeddateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("sworn-date")]
        public MemberDataMemberMemberinfoSworndate sworndate
        {
            get
            {
                return this.sworndateField;
            }
            set
            {
                this.sworndateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("footnote-ref")]
        public byte footnoteref
        {
            get
            {
                return this.footnoterefField;
            }
            set
            {
                this.footnoterefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool footnoterefSpecified
        {
            get
            {
                return this.footnoterefFieldSpecified;
            }
            set
            {
                this.footnoterefFieldSpecified = value;
            }
        }

        /// <remarks/>
        public string footnote
        {
            get
            {
                return this.footnoteField;
            }
            set
            {
                this.footnoteField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMemberMemberinfoState
    {

        private string statefullnameField;

        private string postalcodeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("state-fullname")]
        public string statefullname
        {
            get
            {
                return this.statefullnameField;
            }
            set
            {
                this.statefullnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("postal-code")]
        public string postalcode
        {
            get
            {
                return this.postalcodeField;
            }
            set
            {
                this.postalcodeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMemberMemberinfoElecteddate
    {

        private string dateField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string date
        {
            get
            {
                return this.dateField;
            }
            set
            {
                this.dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMemberMemberinfoSworndate
    {

        private string dateField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string date
        {
            get
            {
                return this.dateField;
            }
            set
            {
                this.dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMemberPredecessorinfo
    {

        private string predlastnameField;

        private string predfirstnameField;

        private string predmiddlenameField;

        private string predofficialnameField;

        private string predformalnameField;

        private object predtitleField;

        private string predmemindexField;

        private string predsortnameField;

        private string predpartyField;

        private MemberDataMemberPredecessorinfoPredvacatedate predvacatedateField;

        private byte predfootnoterefField;

        private string predfootnoteField;

        private string causeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-lastname")]
        public string predlastname
        {
            get
            {
                return this.predlastnameField;
            }
            set
            {
                this.predlastnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-firstname")]
        public string predfirstname
        {
            get
            {
                return this.predfirstnameField;
            }
            set
            {
                this.predfirstnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-middlename")]
        public string predmiddlename
        {
            get
            {
                return this.predmiddlenameField;
            }
            set
            {
                this.predmiddlenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-official-name")]
        public string predofficialname
        {
            get
            {
                return this.predofficialnameField;
            }
            set
            {
                this.predofficialnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-formal-name")]
        public string predformalname
        {
            get
            {
                return this.predformalnameField;
            }
            set
            {
                this.predformalnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-title")]
        public object predtitle
        {
            get
            {
                return this.predtitleField;
            }
            set
            {
                this.predtitleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-memindex")]
        public string predmemindex
        {
            get
            {
                return this.predmemindexField;
            }
            set
            {
                this.predmemindexField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-sort-name")]
        public string predsortname
        {
            get
            {
                return this.predsortnameField;
            }
            set
            {
                this.predsortnameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-party")]
        public string predparty
        {
            get
            {
                return this.predpartyField;
            }
            set
            {
                this.predpartyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-vacate-date")]
        public MemberDataMemberPredecessorinfoPredvacatedate predvacatedate
        {
            get
            {
                return this.predvacatedateField;
            }
            set
            {
                this.predvacatedateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-footnote-ref")]
        public byte predfootnoteref
        {
            get
            {
                return this.predfootnoterefField;
            }
            set
            {
                this.predfootnoterefField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("pred-footnote")]
        public string predfootnote
        {
            get
            {
                return this.predfootnoteField;
            }
            set
            {
                this.predfootnoteField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cause
        {
            get
            {
                return this.causeField;
            }
            set
            {
                this.causeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMemberPredecessorinfoPredvacatedate
    {

        private uint dateField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint date
        {
            get
            {
                return this.dateField;
            }
            set
            {
                this.dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMemberCommitteeassignments
    {

        private MemberDataMemberCommitteeassignmentsCommittee[] committeeField;

        private MemberDataMemberCommitteeassignmentsSubcommittee[] subcommitteeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("committee")]
        public MemberDataMemberCommitteeassignmentsCommittee[] committee
        {
            get
            {
                return this.committeeField;
            }
            set
            {
                this.committeeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("subcommittee")]
        public MemberDataMemberCommitteeassignmentsSubcommittee[] subcommittee
        {
            get
            {
                return this.subcommitteeField;
            }
            set
            {
                this.subcommitteeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMemberCommitteeassignmentsCommittee
    {

        private string comcodeField;

        private string rankField;

        private string leadershipField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string comcode
        {
            get
            {
                return this.comcodeField;
            }
            set
            {
                this.comcodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string rank
        {
            get
            {
                return this.rankField;
            }
            set
            {
                this.rankField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string leadership
        {
            get
            {
                return this.leadershipField;
            }
            set
            {
                this.leadershipField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataMemberCommitteeassignmentsSubcommittee
    {

        private string subcomcodeField;

        private byte rankField;

        private string leadershipField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string subcomcode
        {
            get
            {
                return this.subcomcodeField;
            }
            set
            {
                this.subcomcodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte rank
        {
            get
            {
                return this.rankField;
            }
            set
            {
                this.rankField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string leadership
        {
            get
            {
                return this.leadershipField;
            }
            set
            {
                this.leadershipField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataCommittee
    {

        private string committeefullnameField;

        private MemberDataCommitteeRatio ratioField;

        private MemberDataCommitteeSubcommittee[] subcommitteeField;

        private string typeField;

        private string comcodeField;

        private string comroomField;

        private string comheadertextField;

        private ushort comzipField;

        private ushort comzipsuffixField;

        private string combuildingcodeField;

        private string comphoneField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("committee-fullname")]
        public string committeefullname
        {
            get
            {
                return this.committeefullnameField;
            }
            set
            {
                this.committeefullnameField = value;
            }
        }

        /// <remarks/>
        public MemberDataCommitteeRatio ratio
        {
            get
            {
                return this.ratioField;
            }
            set
            {
                this.ratioField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("subcommittee")]
        public MemberDataCommitteeSubcommittee[] subcommittee
        {
            get
            {
                return this.subcommitteeField;
            }
            set
            {
                this.subcommitteeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string comcode
        {
            get
            {
                return this.comcodeField;
            }
            set
            {
                this.comcodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("com-room")]
        public string comroom
        {
            get
            {
                return this.comroomField;
            }
            set
            {
                this.comroomField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("com-header-text")]
        public string comheadertext
        {
            get
            {
                return this.comheadertextField;
            }
            set
            {
                this.comheadertextField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("com-zip")]
        public ushort comzip
        {
            get
            {
                return this.comzipField;
            }
            set
            {
                this.comzipField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("com-zip-suffix")]
        public ushort comzipsuffix
        {
            get
            {
                return this.comzipsuffixField;
            }
            set
            {
                this.comzipsuffixField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("com-building-code")]
        public string combuildingcode
        {
            get
            {
                return this.combuildingcodeField;
            }
            set
            {
                this.combuildingcodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("com-phone")]
        public string comphone
        {
            get
            {
                return this.comphoneField;
            }
            set
            {
                this.comphoneField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataCommitteeRatio
    {

        private byte majorityField;

        private byte minorityField;

        /// <remarks/>
        public byte majority
        {
            get
            {
                return this.majorityField;
            }
            set
            {
                this.majorityField = value;
            }
        }

        /// <remarks/>
        public byte minority
        {
            get
            {
                return this.minorityField;
            }
            set
            {
                this.minorityField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataCommitteeSubcommittee
    {

        private string subcommitteefullnameField;

        private MemberDataCommitteeSubcommitteeRatio ratioField;

        private string subcomcodeField;

        private string subcomroomField;

        private ushort subcomzipField;

        private byte subcomzipsuffixField;

        private string subcombuildingcodeField;

        private string subcomphoneField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("subcommittee-fullname")]
        public string subcommitteefullname
        {
            get
            {
                return this.subcommitteefullnameField;
            }
            set
            {
                this.subcommitteefullnameField = value;
            }
        }

        /// <remarks/>
        public MemberDataCommitteeSubcommitteeRatio ratio
        {
            get
            {
                return this.ratioField;
            }
            set
            {
                this.ratioField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string subcomcode
        {
            get
            {
                return this.subcomcodeField;
            }
            set
            {
                this.subcomcodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("subcom-room")]
        public string subcomroom
        {
            get
            {
                return this.subcomroomField;
            }
            set
            {
                this.subcomroomField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("subcom-zip")]
        public ushort subcomzip
        {
            get
            {
                return this.subcomzipField;
            }
            set
            {
                this.subcomzipField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("subcom-zip-suffix")]
        public byte subcomzipsuffix
        {
            get
            {
                return this.subcomzipsuffixField;
            }
            set
            {
                this.subcomzipsuffixField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("subcom-building-code")]
        public string subcombuildingcode
        {
            get
            {
                return this.subcombuildingcodeField;
            }
            set
            {
                this.subcombuildingcodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("subcom-phone")]
        public string subcomphone
        {
            get
            {
                return this.subcomphoneField;
            }
            set
            {
                this.subcomphoneField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MemberDataCommitteeSubcommitteeRatio
    {

        private byte majorityField;

        private byte minorityField;

        /// <remarks/>
        public byte majority
        {
            get
            {
                return this.majorityField;
            }
            set
            {
                this.majorityField = value;
            }
        }

        /// <remarks/>
        public byte minority
        {
            get
            {
                return this.minorityField;
            }
            set
            {
                this.minorityField = value;
            }
        }
    }


}
