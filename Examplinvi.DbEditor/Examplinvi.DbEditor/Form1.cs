using Dapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Examplinvi.DbEditor
{
    public partial class Form1 : Form
    {
        private string server=".";
        private string catalog = "tdb";
        private DataTable dt;
        public string connectionstring() => $"server={server}; initial catalog={catalog}; Integrated Security=true;";
        public Form1()
        {
            InitializeComponent();
            SenateMemberLoader.Load(this);
            this.dataGridView1.CellBeginEdit += DataGridView1_CellBeginEdit;
            this.dataGridView1.CellEndEdit += DataGridView1_CellEndEdit;
        }

        long editId;
        int editIndex;
        private void DataGridView1_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var row = e.RowIndex;
            editIndex = 0;
            for(var i=0; i< this.dataGridView1.ColumnCount;i++)
            {
                if (string.Compare(dt.Columns[i].ColumnName, "id", true) == 0)
                {

                    editIndex = i;
                    break;
                }
            }
            editId = (long)this.dataGridView1.Rows[row].Cells[editIndex].Value; 
        }

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var row = e.RowIndex;
            var cell = e.ColumnIndex;
            for (var i = 0; i < this.dataGridView1.Rows.Count; i++)
            {
                if ((long)dataGridView1.Rows[i].Cells[editIndex].Value == editId)
                {
                    row = i;
                    break;
                }
            }
            //
            if (cell == editIndex) return;
            var id = (long)this.dataGridView1.Rows[row].Cells[editIndex].Value;
            var value = this.dataGridView1.Rows[row].Cells[cell].Value;
            var columnName = dt.Columns[cell].ColumnName;

            using(var conn= new SqlConnection(connectionstring()))
            {
                var command = $"update [{dt.TableName}] set [{columnName}] = @value where id=@id";
                conn.Execute(command, new { id, value });
            }
            editId = 0;
           
        }

        bool changingServer = false;
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (changingServer)
                return;
            changingServer = true;
            var idx = cmbServer.SelectedIndex;
            if (idx > -1)
            {
                server = cmbServer.Items[idx].ToString();
                using (var conn = new SqlConnection(connectionstring()))
                {
                    var dbs = conn.Query<string>("select name from sys.databases").ToList();
                    changingDatabases = true;
                    cmbDatabases.Items.Clear();
                    cmbDatabases.Items.Add("");
                    foreach (var db in dbs)
                    {
                        cmbDatabases.Items.Add(db);
                    }
                    changingDatabases = false;

                }
            }

            changingServer = false;

        }
        bool changingDatabases = false;
        private void cmbDatabases_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (changingDatabases)
                return;
            changingDatabases = true;
            var idx = cmbDatabases.SelectedIndex;
            if (idx > -1)
            {
                catalog = cmbDatabases.Items[idx].ToString();
                using (var conn = new SqlConnection(connectionstring()))
                {
                    var tables = conn.Query<string>("select name from sys.tables").ToList();
                    changingTables = true;
                    cmbTable.Items.Clear();
                    cmbTable.Items.Add("");
                    foreach (var table in tables)
                    {
                        cmbTable.Items.Add(table);
                    }
                    changingTables = false;

                }
            }
            changingDatabases = false;
        }

        bool changingTables = false;
        private void cmbTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (changingTables)
                return;
            changingTables = true;

            var idx = cmbTable.SelectedIndex;
            if (idx > 0)
            {
                var table = cmbTable.Items[idx].ToString();
                var query = $"select * from [{table}]";
                using (var conn = new SqlConnection(connectionstring()))
                {
                    using (var adapter = new SqlDataAdapter(query, conn))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        var columns = dt.Columns.Cast<DataColumn>().ToArray();
                        if (!columns.Any(x => string.Compare(x.ColumnName, "id", true) == 0))
                        {
                            MessageBox.Show("Could not find id column");
                        }
                        else
                        {
                            dt.TableName = table;
                            this.dt = dt;
                            this.dataGridView1.DataSource = dt;
                        }


                    }
                }
            }

            changingTables = false;
        }

      
    }
}
