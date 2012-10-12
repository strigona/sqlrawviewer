using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.DtsClient;


namespace RAW_File_Viewer
{
    public partial class frmMain : Form
    {
        clsDataFlow _objDataFlow = new clsDataFlow();

        public frmMain()
        {
            InitializeComponent();
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void menuItemOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Open RAW File";
            fDialog.Filter = "RAW Files|*.RAW|All Files|*.*";
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                BindDataGridView(fDialog.FileName.ToString());
            }
        }

        // Loads RAWData to DataGridView
        internal void BindDataGridView(string strRawFileName)
        {
            try
            {
                _objDataFlow = new clsDataFlow();
                _objDataFlow.strRAWFileName = strRawFileName;
                // Creates Package for DataFlow
                _objDataFlow.CreatePackage();
                // Creates Source Component - DataFlow
                _objDataFlow.CreateSourceComponent(_objDataFlow.strRAWFileName);

                // Creates Destination Component - DataFlow
                _objDataFlow.CreateDestinationReaderComponent();
                String strPath = _objDataFlow.SavePackage();

                DataSet dsGridView = GetGridViewData(strPath);

                if (dsGridView != null)
                {
                    dgvMain.Enabled = true;
                    dgvMain.DataSource = dsGridView;
                    dgvMain.DataMember = dsGridView.Tables[0].TableName;
                    // Set tooltip for header to be the column's data type
                    foreach (DataGridViewColumn dgvColumn in dgvMain.Columns)
                    {
                        dgvColumn.HeaderCell.ToolTipText = dsGridView.Tables[0].Columns[dgvColumn.Index].DataType.ToString();
                    }
                }
                File.Delete(strPath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error processing file. Selected file may be wrong type or corrupted.");
            }
            GC.Collect();
        }

        internal DataSet GetGridViewData(string strPath)
        {
            string dtexecArgs;
            string dataReaderName;
            DtsConnection dtsConnection;
            DtsCommand dtsCommand;

            DataSet dsPackageData = new DataSet();
            IDataReader dtsDataReader;
            dtexecArgs = string.Format("-f \"{0}\"", strPath);

            dataReaderName = "DataReaderDest";
            dtsConnection = new DtsConnection();
            dtsConnection.ConnectionString = dtexecArgs;
            dtsCommand = new DtsCommand(dtsConnection);
            dtsCommand.CommandText = dataReaderName;
            dtsConnection.Open();
            dtsDataReader = dtsCommand.ExecuteReader(CommandBehavior.Default);

            dsPackageData.Load(dtsDataReader, LoadOption.OverwriteChanges, dtsDataReader.GetSchemaTable().TableName);

            try
            {
                if (dtsDataReader != null)
                {
                    dtsDataReader.Close();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    string.Format("Exception closing DataReader: {0}{1}", e.InnerException.Message, Environment.NewLine),
                    "Exception closing connection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );
            }
            return dsPackageData;
        }

    }

}
