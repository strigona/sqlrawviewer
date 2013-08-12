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
        internal enum SearchType { Columns, Rows, Column, Row };

        clsDataFlow _objDataFlow = new clsDataFlow();
        internal string _strWindowTitle;
        bool _bFileOpen = false;
        #region Search Members
        String _strPreviousSearch = "";
        DataGridViewCell _dgvcStartCell;
        DataGridViewCell _dgvcPreviousSearchResultCell;
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
                    SearchText(toolStripTextFind.Text, (SearchType)Enum.Parse(typeof(SearchType), toolStripComboSearchBy.SelectedItem.ToString()));
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.toolStripComboSearchBy.Items.AddRange(Enum.GetNames(typeof(SearchType)));
            this.toolStripComboSearchBy.SelectedItem = Properties.Settings.Default.SearchBy;
            try
            {
                Enum.Parse(typeof(SearchType), Properties.Settings.Default.SearchBy);
            }
            catch (Exception)
            {
                this.toolStripComboSearchBy.SelectedIndex = 0;
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void toolStripComboSearchBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SearchBy = ((ToolStripComboBox)sender).SelectedItem.ToString();
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
                SearchText(toolStripTextFind.Text, (SearchType)Enum.Parse(typeof(SearchType), toolStripComboSearchBy.SelectedItem.ToString()));
                e.Handled = true;
            }
        }

        private void toolStripButtonFind_Click(object sender, EventArgs e)
        {
            SearchText(toolStripTextFind.Text, (SearchType)Enum.Parse(typeof(SearchType), toolStripComboSearchBy.SelectedItem.ToString()));
        }
        #endregion

        #region Search
        internal bool SearchText(string strSearchText, SearchType searchType)
        {
            Console.Out.WriteLine("Search: " + searchType);
            DataGridViewCell currentCell;
            if (strSearchText.Equals(_strPreviousSearch))
            {
                // Continue searching after last search result
                currentCell = getNextSearchCell(_dgvcPreviousSearchResultCell, searchType);
            }
            else
            {
                clearCellHighlight(_dgvcPreviousSearchResultCell);
                _dgvcPreviousSearchResultCell = null;
                _dgvcStartCell = currentCell = dgvMain.CurrentCell;
                _strPreviousSearch = strSearchText;
            }

            while (!currentCell.Value.ToString().Contains(strSearchText))
            {
                currentCell = getNextSearchCell(currentCell, searchType);
                if (_dgvcStartCell.RowIndex == currentCell.RowIndex && _dgvcStartCell.ColumnIndex == currentCell.ColumnIndex)
                {
                    displayEndOfSearchMessage(searchType, _dgvcPreviousSearchResultCell == null);
                    clearCellHighlight(_dgvcPreviousSearchResultCell);
                    _dgvcPreviousSearchResultCell = null;
                    return false;
                }
            }

            clearCellHighlight(_dgvcPreviousSearchResultCell);
            highlightCell(currentCell);
            _dgvcPreviousSearchResultCell = currentCell;
            dgvMain.CurrentCell = currentCell;
        
            return true;
        }

        // Build and display end of search message
        internal void displayEndOfSearchMessage(SearchType searchType, bool foundText)
        {
            string strMessage = "";
            if (_dgvcPreviousSearchResultCell == null)
            {
                // If it isn't found on the first search through the document it doesn't exist at all
                strMessage = "Text not found in the ";
            }
            else
            {
                // We found something on a previous search
                strMessage = "Finished searching this ";
            }
            switch (searchType)
            {
                case SearchType.Column:
                    strMessage += "column";
                    break;
                case SearchType.Row:
                    strMessage += "row";
                    break;
                default:
                    // Columns and Rows still search the whole file
                    strMessage += "file";
                    break;
            }

            MessageBox.Show(strMessage);
        }

        internal void clearCellHighlight(DataGridViewCell cell)
        {
            if (cell != null)
            {
                cell.Style.BackColor = Color.Empty;
            }
        }

        internal void highlightCell(DataGridViewCell cell)
        {
            if (cell != null)
            {
                cell.Style.BackColor = Color.Yellow;
            }
        }

        internal DataGridViewCell getNextSearchCell(DataGridViewCell currentCell, SearchType searchType)
        {
            switch (searchType)
            {
                case SearchType.Column:
                    return getNextSearchCellColumn(currentCell, true);
                case SearchType.Columns:
                    return getNextSearchCellColumn(currentCell, false);
                case SearchType.Row:
                    return getNextSearchCellRow(currentCell, true);
                case SearchType.Rows:
                    return getNextSearchCellRow(currentCell, false);
                default:
                    throw new Exception("Invalid parameter value for searchType.");
            }
        }

        internal DataGridViewCell getNextSearchCellRow(DataGridViewCell currentCell, bool limitToColumn)
        {
            int row = currentCell.RowIndex;
            int column = (currentCell.ColumnIndex + 1) % dgvMain.Columns.Count;
            if (column == 0 && !limitToColumn)
            {
                row = (currentCell.RowIndex + 1) % dgvMain.Rows.Count;
            }
            return dgvMain.Rows[row].Cells[column];
        }

        internal DataGridViewCell getNextSearchCellColumn(DataGridViewCell currentCell, bool limitToRow)
        {
            int column = currentCell.ColumnIndex;
            int row = (currentCell.RowIndex + 1) % dgvMain.Rows.Count;
            if (row == 0 && !limitToRow)
            {
                column = (currentCell.ColumnIndex + 1) % dgvMain.Columns.Count;
            }
            return dgvMain.Rows[row].Cells[column];
        }
        #endregion

        #region File
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
        #endregion

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
