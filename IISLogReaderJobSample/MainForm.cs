using System;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using EasyETL.DataSets;
using EasyETL.Listeners;
using EasyETL.Parsers;
using System.Messaging;
using System.Diagnostics;

namespace IISLogReaderJobSample
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            cmbProfile.Items.Clear();
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("profiles.xml");
            if (xDoc.SelectSingleNode("profiles") != null)
            {
                foreach (XmlNode xNode in xDoc.SelectSingleNode("profiles").ChildNodes)
                {
                    cmbProfile.Items.Add(xNode.Name);
                }
            }
        }



        private void LoadData()
        {
            lblProgressMessage.Text = "";
            dgParsedData.DataSource = null;

            EasyETL.EasyETLJob job = new EasyETL.EasyETLJob();

            job.Listeners.Add(new FileListener(this, txtFileName.Text));

            Extractor p = new Extractor(txtFileName.Text);
            p.LoadProfile(cmbProfile.Text);
            job.Extractors.Add(p);

            job.RowReadAndProcessed += job_LineReadAndProcessed;
            job.DataChanged += job_DataChanged;
            job.Start();

            RegexDataSet rds = (RegexDataSet)job.Data;

            cmbParsedDataSet.Items.Clear();

            foreach (DataTable dt in rds.Tables)
            {
                cmbParsedDataSet.Items.Add(dt.TableName);
            }

            if (cmbParsedDataSet.Items.Count > 0)
            {
                cmbParsedDataSet.Text = cmbParsedDataSet.Items[0].ToString();
                cmbParsedDataSet.SelectedIndex = 0;
            }

            dgParsedData.DataSource = rds;

            rtFailedRecords.Text = (rds.MisReads == null) ? "" : String.Join(Environment.NewLine, rds.MisReads);
            lblProgressMessage.Text = "";
        }

        void job_DataChanged(object sender, EasyETL.JobDataChangedEventArgs e)
        {
            dgParsedData.Invoke(new MethodInvoker(() => { dgParsedData.Refresh(); }));
        }

        private void job_LineReadAndProcessed(object sender, RowReadEventArgs e)
        {
            int result = 0;
            //Math.DivRem(e.LineNumber, 1000, out result);
            if (result == 0)
            {
                lblProgressMessage.Text = e.Message + "(" + e.RowNumber.ToString() + ")";
                Application.DoEvents();
            }
        }

        private void ofdButton_Click_1(object sender, EventArgs e)
        {
            if (ofdBox.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtFileName.Text = ofdBox.FileName;
            }
        }

        private void cmbParsedDataSet_SelectedIndexChanged(object sender, EventArgs e)
        {
            dgParsedData.DataMember = cmbParsedDataSet.Text;
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            this.UseWaitCursor = true;
            DateTime StartTime = DateTime.Now;
            LoadData();
            this.UseWaitCursor = false;
            MessageBox.Show(String.Format("Parsed {0} Records successfully and Failed {1} records in {2}  Seconds", dgParsedData.RowCount, rtFailedRecords.Lines.Length, DateTime.Now.Subtract(StartTime).TotalSeconds.ToString()));
        }

    }
}