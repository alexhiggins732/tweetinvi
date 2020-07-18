using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Examplinvi.WinFormsApp
{
    public partial class UrlInput : Form
    {
        public List<Uri> URIs;
        public UrlInput()
        {
            InitializeComponent();
            this.URIs = new List<Uri>();
        }

        private void UrlInput_Load(object sender, EventArgs e)
        {

        }
        public TextBox TextBox => this.txtUrls;

        private void btnSave_Click(object sender, EventArgs e)
        {
            var lines = this.txtUrls.Lines;
            foreach(var line in lines)
            {
                if(Uri.TryCreate(line, UriKind.Absolute, out Uri uri))
                {
                    this.URIs.Add(uri);
                }
            }
            this.DialogResult = DialogResult.OK;
        }
    }
}
