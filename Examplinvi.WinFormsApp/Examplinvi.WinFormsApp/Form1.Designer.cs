namespace Examplinvi.WinFormsApp
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autofollowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.retweetWithMediaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.retweetWithMediaToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.loadTweetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.txtinput = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnLoad = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbDownload = new System.Windows.Forms.RadioButton();
            this.rbUpdate = new System.Windows.Forms.RadioButton();
            this.rbImage = new System.Windows.Forms.RadioButton();
            this.rbVideo = new System.Windows.Forms.RadioButton();
            this.rbNormal = new System.Windows.Forms.RadioButton();
            this.txtRawText = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(995, 40);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(72, 36);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(199, 44);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autofollowToolStripMenuItem,
            this.retweetWithMediaToolStripMenuItem,
            this.retweetWithMediaToolStripMenuItem1,
            this.loadTweetToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(90, 36);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // autofollowToolStripMenuItem
            // 
            this.autofollowToolStripMenuItem.Name = "autofollowToolStripMenuItem";
            this.autofollowToolStripMenuItem.Size = new System.Drawing.Size(360, 44);
            this.autofollowToolStripMenuItem.Text = "Autofollow";
            this.autofollowToolStripMenuItem.Click += new System.EventHandler(this.autofollowToolStripMenuItem_Click);
            // 
            // retweetWithMediaToolStripMenuItem
            // 
            this.retweetWithMediaToolStripMenuItem.Name = "retweetWithMediaToolStripMenuItem";
            this.retweetWithMediaToolStripMenuItem.Size = new System.Drawing.Size(360, 44);
            this.retweetWithMediaToolStripMenuItem.Text = "Retweet with Video";
            this.retweetWithMediaToolStripMenuItem.Click += new System.EventHandler(this.retweetWithVideoToolStripMenuItem_Click);
            // 
            // retweetWithMediaToolStripMenuItem1
            // 
            this.retweetWithMediaToolStripMenuItem1.Name = "retweetWithMediaToolStripMenuItem1";
            this.retweetWithMediaToolStripMenuItem1.Size = new System.Drawing.Size(360, 44);
            this.retweetWithMediaToolStripMenuItem1.Text = "Retweet with Media";
            this.retweetWithMediaToolStripMenuItem1.Click += new System.EventHandler(this.retweetWithMediaToolStripMenuItem1_Click);
            // 
            // loadTweetToolStripMenuItem
            // 
            this.loadTweetToolStripMenuItem.Name = "loadTweetToolStripMenuItem";
            this.loadTweetToolStripMenuItem.Size = new System.Drawing.Size(360, 44);
            this.loadTweetToolStripMenuItem.Text = "Load Tweet";
            this.loadTweetToolStripMenuItem.Click += new System.EventHandler(this.loadTweetToolStripMenuItem_Click);
            // 
            // txtinput
            // 
            this.txtinput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtinput.Location = new System.Drawing.Point(63, 63);
            this.txtinput.Name = "txtinput";
            this.txtinput.Size = new System.Drawing.Size(804, 31);
            this.txtinput.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 63);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "Url:";
            // 
            // btnLoad
            // 
            this.btnLoad.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoad.Location = new System.Drawing.Point(891, 57);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(86, 42);
            this.btnLoad.TabIndex = 3;
            this.btnLoad.Text = "Load";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.rbDownload);
            this.groupBox1.Controls.Add(this.rbUpdate);
            this.groupBox1.Controls.Add(this.rbImage);
            this.groupBox1.Controls.Add(this.rbVideo);
            this.groupBox1.Controls.Add(this.rbNormal);
            this.groupBox1.Location = new System.Drawing.Point(17, 116);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(960, 100);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Options";
            // 
            // rbDownload
            // 
            this.rbDownload.AutoSize = true;
            this.rbDownload.Location = new System.Drawing.Point(462, 45);
            this.rbDownload.Name = "rbDownload";
            this.rbDownload.Size = new System.Drawing.Size(138, 29);
            this.rbDownload.TabIndex = 4;
            this.rbDownload.TabStop = true;
            this.rbDownload.Text = "Download";
            this.rbDownload.UseVisualStyleBackColor = true;
            // 
            // rbUpdate
            // 
            this.rbUpdate.AutoSize = true;
            this.rbUpdate.Location = new System.Drawing.Point(344, 45);
            this.rbUpdate.Name = "rbUpdate";
            this.rbUpdate.Size = new System.Drawing.Size(112, 29);
            this.rbUpdate.TabIndex = 3;
            this.rbUpdate.TabStop = true;
            this.rbUpdate.Text = "Update";
            this.rbUpdate.UseVisualStyleBackColor = true;
            // 
            // rbImage
            // 
            this.rbImage.AutoSize = true;
            this.rbImage.Location = new System.Drawing.Point(237, 45);
            this.rbImage.Name = "rbImage";
            this.rbImage.Size = new System.Drawing.Size(101, 29);
            this.rbImage.TabIndex = 2;
            this.rbImage.TabStop = true;
            this.rbImage.Text = "Image";
            this.rbImage.UseVisualStyleBackColor = true;
            // 
            // rbVideo
            // 
            this.rbVideo.AutoSize = true;
            this.rbVideo.Location = new System.Drawing.Point(133, 45);
            this.rbVideo.Name = "rbVideo";
            this.rbVideo.Size = new System.Drawing.Size(98, 29);
            this.rbVideo.TabIndex = 1;
            this.rbVideo.TabStop = true;
            this.rbVideo.Text = "Video";
            this.rbVideo.UseVisualStyleBackColor = true;
            // 
            // rbNormal
            // 
            this.rbNormal.AutoSize = true;
            this.rbNormal.Location = new System.Drawing.Point(16, 45);
            this.rbNormal.Name = "rbNormal";
            this.rbNormal.Size = new System.Drawing.Size(111, 29);
            this.rbNormal.TabIndex = 0;
            this.rbNormal.TabStop = true;
            this.rbNormal.Text = "Normal";
            this.rbNormal.UseVisualStyleBackColor = true;
            // 
            // txtRawText
            // 
            this.txtRawText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRawText.Location = new System.Drawing.Point(17, 233);
            this.txtRawText.Multiline = true;
            this.txtRawText.Name = "txtRawText";
            this.txtRawText.Size = new System.Drawing.Size(960, 326);
            this.txtRawText.TabIndex = 5;
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Location = new System.Drawing.Point(868, 581);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(109, 44);
            this.btnSend.TabIndex = 6;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 644);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(995, 42);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(84, 32);
            this.toolStripStatusLabel1.Text = "Status:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(995, 686);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtRawText);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtinput);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Exampleinvi";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autofollowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem retweetWithMediaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem retweetWithMediaToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem loadTweetToolStripMenuItem;
        private System.Windows.Forms.TextBox txtinput;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbImage;
        private System.Windows.Forms.RadioButton rbVideo;
        private System.Windows.Forms.RadioButton rbNormal;
        private System.Windows.Forms.TextBox txtRawText;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.RadioButton rbUpdate;
        private System.Windows.Forms.RadioButton rbDownload;
    }
}

