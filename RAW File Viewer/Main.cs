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
        internal string _strWindowTitle;
        bool _bFileOpen = false;
        #region Search Members
        DataGridViewCell _dgcPreviousSearchCell = null;
        String _strPreviousSearch = "";
        int _iStartRowIndex = 0;
        int _iStartColumnIndex = 0;
        #endregion
        public frmMain(string[] args)
        {
            InitializeComponent();
            _strWindowTitle = this.Text;
            if (args.Length != 0)
            {
                OpenFile(args[0]);
            }
        }

        #region Events
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                if (this._bFileOpen)
                {
                    SearchText(toolStripTextFind.Text);
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void toolStripButtonOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog();
        }

        private void menuItemOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog();
        }

        private void menuItemClose_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        private void toolStripTextFind_CheckEnter(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) Keys.Enter)
            {
                SearchText(toolStripTextFind.Text);
                e.Handled = true;
            }
        }

        private void toolStripButtonFind_Click(object sender, EventArgs e)
        {
            SearchText(toolStripTextFind.Text);
        }
        #endregion

        internal void OpenFileDialog()
        {
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Open RAW File";
            fDialog.Filter = "RAW Files|*.RAW|All Files|*.*";
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                OpenFile(fDialog.FileName.ToString());
            }
        }

        internal bool SearchText(string strSearchText)
        {
            DataGridViewCell dgvcSearchCell = null;
            if (this._bFileOpen && strSearchText != "" && dgvMain.CurrentCell != null)
            {
                bool bFirstSearch;
                int iStartRowIndex = dgvMain.CurrentCell.RowIndex;
                int iStartColumnIndex = dgvMain.CurrentCell.ColumnIndex;
                // Clear highlighted cell
                if (this._dgcPreviousSearchCell != null)
                {
                    iStartRowIndex = this._dgcPreviousSearchCell.RowIndex;
                    iStartColumnIndex = this._dgcPreviousSearchCell.ColumnIndex;
                    this._dgcPreviousSearchCell.Style.BackColor = Color.Empty;
                    this._dgcPreviousSearchCell = null;
                }

                if (!this._strPreviousSearch.Equals(strSearchText))
                {
                    // Reset search start position if new search string
                    this._strPreviousSearch = strSearchText;
                    this._iStartRowIndex = dgvMain.CurrentCell.RowIndex;
                    this._iStartColumnIndex = dgvMain.CurrentCell.ColumnIndex;
                    bFirstSearch = true;
                    iStartRowIndex = dgvMain.CurrentCell.RowIndex;
                    iStartColumnIndex = dgvMain.CurrentCell.ColumnIndex;
                }
                else
                {
                    // Otherwise move passed current cell
                    bFirstSearch = false;
                    iStartRowIndex = dgvMain.CurrentCell.RowIndex;
                    iStartColumnIndex = (dgvMain.CurrentCell.ColumnIndex + 1) % dgvMain.Columns.Count;
                    if (iStartColumnIndex == 0)
                    {
                        iStartRowIndex = (dgvMain.CurrentCell.RowIndex + 1) % dgvMain.Rows.Count;
                    }
                }
                // Search the remainder of the columns in the current row
                for (int iColumnIndex = iStartColumnIndex; iColumnIndex < dgvMain.Rows[iStartRowIndex].Cells.Count; iColumnIndex++)
                {
                    if (_iStartRowIndex == iStartRowIndex && _iStartColumnIndex == iColumnIndex && !bFirstSearch)
                    {
                        // Searched the whole grid
                        MessageBox.Show("Finished searching this file");
                        _strPreviousSearch = "";
                        return false;
                    }
                    dgvcSearchCell = dgvMain.Rows[iStartRowIndex].Cells[iColumnIndex];
                    if (dgvcSearchCell.Value.ToString().Contains(strSearchText))
                    {
                        dgvcSearchCell.Style.BackColor = Color.Yellow;
                        dgvMain.CurrentCell = dgvcSearchCell;
                        this._dgcPreviousSearchCell = dgvcSearchCell;
                        return true;
                    }
                }
                // Search all rows except for the start row
                for (int iRowIndex = (iStartRowIndex + 1) % dgvMain.Rows.Count; iRowIndex != this._iStartRowIndex; iRowIndex = (iRowIndex + 1) % dgvMain.Rows.Count)
                {
                    for (int iColumnIndex = 0; iColumnIndex < dgvMain.Rows[iRowIndex].Cells.Count; iColumnIndex++)
                    {
                        dgvcSearchCell = dgvMain.Rows[iRowIndex].Cells[iColumnIndex];
                        if (dgvcSearchCell.Value.ToString().Contains(strSearchText))
                        {
                            dgvcSearchCell.Style.BackColor = Color.Yellow;
                            dgvMain.CurrentCell = dgvcSearchCell;
                            this._dgcPreviousSearchCell = dgvcSearchCell;
                            return true;
                        }
                    }
                }
                // Search the beginning columns on the start row
                for (int iColumnIndex = 0; iColumnIndex < _iStartColumnIndex; iColumnIndex++)
                {
                    dgvcSearchCell = dgvMain.Rows[_iStartRowIndex].Cells[iColumnIndex];
                    if (dgvcSearchCell.Value.ToString().Contains(strSearchText))
                    {
                      dgvcSearchCell.Style.BackColor = Color.Yellow;
                        dgvMain.CurrentCell = dgvcSearchCell;
                        this._dgcPreviousSearchCell = dgvcSearchCell;
                        return true;
                    }
                }
                // Text isn't in the file or we've found every occurrence, clear _strPreviousSearch
                // so next search will start from scratch
                _strPreviousSearch = "";
                if (bFirstSearch)
                {
                    // If it isn't found on the first search through the document it doesn't exist at all
                    MessageBox.Show("Text not found in the document");
                }
                else
                {
                    // We found something on a previous search
                    MessageBox.Show("Finished searching this file");
                }
            }

            return false;
        }

        internal void CloseFile()
        {
            dgvMain.DataSource = null;
            menuItemClose.Enabled = false;
            toolStripButtonFind.Enabled = false;
            toolStripTextFind.Enabled = false;
            this.Text = String.Format("{0}", _strWindowTitle);
            this._bFileOpen = false;
            GC.Collect();
        }

        // Loads RAWData to DataGridView
        internal void OpenFile(string strRawFileName)
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
                    //  and set default width to column header width
                    foreach (DataGridViewColumn dgvColumn in dgvMain.Columns)
                    {
                        dgvColumn.HeaderCell.ToolTipText = dsGridView.Tables[0].Columns[dgvColumn.Index].DataType.ToString();
                        dgvColumn.Width = dgvColumn.HeaderCell.PreferredSize.Width;
                    }
                }
                File.Delete(strPath);
                // Set titlebar
                this.Text = String.Format("{0} - {1} (in {2})",
                    _strWindowTitle,
                    System.IO.Path.GetFileName(strRawFileName),
                    System.IO.Path.GetDirectoryName(strRawFileName));
                menuItemClose.Enabled = true;
                toolStripButtonFind.Enabled = true;
                toolStripTextFind.Enabled = true;
                this._bFileOpen = true;
            }
            catch (Exception)
            {
                MessageBox.Show("Error opening file. Selected file may be wrong type or corrupted.");
                CloseFile();
            }
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
