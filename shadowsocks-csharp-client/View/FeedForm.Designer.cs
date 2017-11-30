namespace Shadowsocks.View
{
    partial class FeedForm
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
            this.feedList = new System.Windows.Forms.ListBox();
            this.delete = new System.Windows.Forms.Button();
            this.add = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.save = new System.Windows.Forms.Button();
            this.export = new System.Windows.Forms.Button();
            this.import = new System.Windows.Forms.Button();
            this.openSSF = new System.Windows.Forms.OpenFileDialog();
            this.saveSSF = new System.Windows.Forms.SaveFileDialog();
            this.SuspendLayout();
            // 
            // feedList
            // 
            this.feedList.FormattingEnabled = true;
            this.feedList.Location = new System.Drawing.Point(12, 12);
            this.feedList.Name = "feedList";
            this.feedList.Size = new System.Drawing.Size(360, 147);
            this.feedList.TabIndex = 0;
            this.feedList.SelectedIndexChanged += new System.EventHandler(this.feedList_SelectedIndexChanged);
            // 
            // delete
            // 
            this.delete.Location = new System.Drawing.Point(12, 226);
            this.delete.Name = "delete";
            this.delete.Size = new System.Drawing.Size(116, 23);
            this.delete.TabIndex = 1;
            this.delete.Text = "&Delete";
            this.delete.UseVisualStyleBackColor = true;
            this.delete.Click += new System.EventHandler(this.delete_Click);
            // 
            // add
            // 
            this.add.Location = new System.Drawing.Point(256, 226);
            this.add.Name = "add";
            this.add.Size = new System.Drawing.Size(116, 23);
            this.add.TabIndex = 2;
            this.add.Text = "&Add";
            this.add.UseVisualStyleBackColor = true;
            this.add.Click += new System.EventHandler(this.add_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 171);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(360, 20);
            this.textBox1.TabIndex = 3;
            // 
            // save
            // 
            this.save.Location = new System.Drawing.Point(134, 226);
            this.save.Name = "save";
            this.save.Size = new System.Drawing.Size(116, 23);
            this.save.TabIndex = 4;
            this.save.Text = "&Save";
            this.save.UseVisualStyleBackColor = true;
            this.save.Click += new System.EventHandler(this.save_Click);
            // 
            // export
            // 
            this.export.Location = new System.Drawing.Point(12, 197);
            this.export.Name = "export";
            this.export.Size = new System.Drawing.Size(177, 23);
            this.export.TabIndex = 5;
            this.export.Text = "&Export";
            this.export.UseVisualStyleBackColor = true;
            this.export.Click += new System.EventHandler(this.export_Click);
            // 
            // import
            // 
            this.import.Location = new System.Drawing.Point(195, 197);
            this.import.Name = "import";
            this.import.Size = new System.Drawing.Size(177, 23);
            this.import.TabIndex = 6;
            this.import.Text = "&Import";
            this.import.UseVisualStyleBackColor = true;
            this.import.Click += new System.EventHandler(this.import_Click);
            // 
            // openSSF
            // 
            this.openSSF.DefaultExt = "ssf";
            this.openSSF.Filter = "*.ssf|*.ssf";
            // 
            // saveSSF
            // 
            this.saveSSF.DefaultExt = "ssf";
            this.saveSSF.FileName = "feeds.ssf";
            this.saveSSF.Filter = "*.ssf|*.ssf";
            this.saveSSF.RestoreDirectory = true;
            // 
            // FeedForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 261);
            this.Controls.Add(this.import);
            this.Controls.Add(this.export);
            this.Controls.Add(this.save);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.add);
            this.Controls.Add(this.delete);
            this.Controls.Add(this.feedList);
            this.MaximumSize = new System.Drawing.Size(400, 300);
            this.MinimumSize = new System.Drawing.Size(400, 300);
            this.Name = "FeedForm";
            this.Text = "FeedForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox feedList;
        private System.Windows.Forms.Button delete;
        private System.Windows.Forms.Button add;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button save;
        private System.Windows.Forms.Button export;
        private System.Windows.Forms.Button import;
        private System.Windows.Forms.OpenFileDialog openSSF;
        private System.Windows.Forms.SaveFileDialog saveSSF;
    }
}