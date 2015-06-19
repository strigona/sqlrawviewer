// Description: SSIS 2008 Package Programming to move data from the RAWFile to DataGridView
//              using Data Reader as Destination
// Created by:  Mitulkumar Brahmbhatt
// Modified by: Simon Trigona
//*************************************************************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.DtsClient;

namespace RAW_File_Viewer
{

    internal class clsDataFlow
    {
    #region Private Members

        // Package, Pipe, Connection Members
        private Microsoft.SqlServer.Dts.Runtime.Wrapper.PackageClass _objPackage = new Microsoft.SqlServer.Dts.Runtime.Wrapper.PackageClass();
        private MainPipe _objMainPipe = null;
        private IDTSPath100 _objIDTSPath = null;
        private Microsoft.SqlServer.Dts.Runtime.Wrapper.Application app = null;

        // Metadata Members
        private Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSComponentMetaData100 _objIDTSSRCMetaData;
        private Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSComponentMetaData100 _objIDTSDSTReaderMetaData;

        // Wrapper Members
        private CManagedComponentWrapper _objSourceWrapper;
        private CManagedComponentWrapper _objDestinationReaderWrapper;

        // Moniker & Constants
        private const string _strDataFlowTaskMoniker = "STOCK:PipelineTask";

        // DataFlow Component Id
        private const string _strSourceDFComponentID = "DTSAdapter.RawSource.3";
        private const string _strDestinationDFReaderComponentID = "Microsoft.SqlServer.Dts.Pipeline.DataReaderDestinationAdapter, Microsoft.SqlServer.DataReaderDest, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";

        // Other Members
        private string _strRAWFileName = string.Empty;
        private string _strPKGFileName = string.Empty;
        private Microsoft.SqlServer.Dts.Runtime.Wrapper.IDTSVariable100 _dataVariable = null;
        private StringBuilder _strRAWColNames = new StringBuilder();
        private char _delim = ',';
        private int _iDestInputID = 0;
        private string _strIndex = string.Empty;
        private int _iIndex = 0;
        private IDTSInput100 _objIDTSInput = null;
        private IDTSVirtualInput100 _objIDTSVirtualInput = null;
        private string[] _strFilterArray = null;

    #endregion
    #region Constructor

        internal clsDataFlow()
        {
            // Constructor
        }

    #endregion
    #region Internal Members/Properties

        internal string strRAWFileName
        {
            get { return this._strRAWFileName; }
            set { this._strRAWFileName = value; }
        }

        internal string strPKGFileName
        {
            get { return this._strPKGFileName; }
            set { this._strPKGFileName = value; }
        }

        internal string strRAWColNames
        {
            get { return this._strRAWColNames.ToString(); }
            set { this._strRAWColNames.Append(value); }
        }

    #endregion
    #region Creates Package

        // Creates Runtime Package
        internal void CreatePackage()
        {
            _objPackage = new Microsoft.SqlServer.Dts.Runtime.Wrapper.PackageClass();
            _objPackage.CreationDate = DateTime.Now;
            _objPackage.ProtectionLevel = Microsoft.SqlServer.Dts.Runtime.Wrapper.DTSProtectionLevel.DTSPL_DONTSAVESENSITIVE;
            _objPackage.Name = "RAWReader";
            _objPackage.Description = "RAW To Reader Conversion Package";
            _objPackage.DelayValidation = false;
            _objPackage.PackageType = DTSPackageType.DTSPKT_DTSDESIGNER100;
            _dataVariable = _objPackage.Variables.Add("DataRecords", false, "User", new System.Object());
        }

    #endregion
    #region Call Dataflow Component
        #region Source and Destination Component Methods

        // Creates Source Component (Output Collection)
        internal void CreateSourceComponent()
        {
            // Creates mainpipe for the executable component
            _objMainPipe = ((TaskHost)_objPackage.Executables.Add(_strDataFlowTaskMoniker)).InnerObject as MainPipe;
            // Adds a component from the MainPipe to the Source Metadata
            _objIDTSSRCMetaData = _objMainPipe.ComponentMetaDataCollection.New();
            // Sets the source component class id
            _objIDTSSRCMetaData.ComponentClassID = _strSourceDFComponentID;
            // Sets the locale property
            _objIDTSSRCMetaData.LocaleID = -1;
            // Instantiates the Wrapper, adding Source Metadata
            _objSourceWrapper = _objIDTSSRCMetaData.Instantiate();
            // Provides default properties
            _objSourceWrapper.ProvideComponentProperties();
            // Sets RAWFile Component Property
            _objSourceWrapper.SetComponentProperty("AccessMode", 0);
            _objSourceWrapper.SetComponentProperty("FileName", strRAWFileName);
            _objSourceWrapper.SetComponentProperty("FileNameVariable", null);
            // Sets the connection
            _objSourceWrapper.AcquireConnections(null);
            // Reinitializes the Source Metadata
            _objSourceWrapper.ReinitializeMetaData();
            // Fetch ColumnNames for the Metadata
            if (_strRAWColNames.Length == 0 && _strRAWColNames.ToString() == string.Empty)
            {
                foreach (IDTSOutputColumn100 idtsOutPutColumn in _objIDTSSRCMetaData.OutputCollection[0].OutputColumnCollection)
                {
                    _strRAWColNames.Append(idtsOutPutColumn + ",");
                }
            }
            // Releases the Wrapper connection
            _objSourceWrapper.ReleaseConnections();
        }

        // Creates Destination Component (Input Collection)
        internal void CreateDestinationReaderComponent()
        {
            // 1. DataReader String: the class name of the DataReader destination.
            // 2. FailOnTimeout Boolean:Indicates wheather to fail when a ReadTimeout occurs. The default value is False.
            // 3. ReadeTimeout Integer: The number of milliseconds before a timeout occurs. The default value of this property is 30000 (30 seconds).

            // Adds a component from MainPipe to the Destination Recordset Metadata
            _objIDTSDSTReaderMetaData = _objMainPipe.ComponentMetaDataCollection.New();
            // Sets the Destination recordset component name
            _objIDTSDSTReaderMetaData.Name = "Test";
            // Sets the Destination recordset component class id
            _objIDTSDSTReaderMetaData.ComponentClassID = _strDestinationDFReaderComponentID;

            IDTSCustomProperty100 _property = _objIDTSDSTReaderMetaData.CustomPropertyCollection.New();
            _property.Name = "DataReader";
            _property.Value = new object();

            _property = _objIDTSDSTReaderMetaData.CustomPropertyCollection.New();
            _property.Name = "FailOnTimeout";
            _property.Value = false;

            _property = _objIDTSDSTReaderMetaData.CustomPropertyCollection.New();
            _property.Name = "ReadTimeout";
            _property.Value = 30000;

            // Instantiates the Wrapper adding Destination Recordset Metadata
            _objDestinationReaderWrapper = _objIDTSDSTReaderMetaData.Instantiate();
            // Provides default properties
            _objDestinationReaderWrapper.ProvideComponentProperties();
            // Sets the connection
            _objDestinationReaderWrapper.AcquireConnections(null);
            // Reinitializes the Destination Metadata
            _objDestinationReaderWrapper.ReinitializeMetaData();
            // Releases the Wrapper connection
            _objDestinationReaderWrapper.ReleaseConnections();
            // Creates the IDTSPath from the MainPipe
            _objIDTSPath = _objMainPipe.PathCollection.New();


            _objIDTSPath.AttachPathAndPropagateNotifications(_objIDTSSRCMetaData.OutputCollection[0], _objIDTSDSTReaderMetaData.InputCollection[0]);
            _objIDTSInput = _objIDTSDSTReaderMetaData.InputCollection[0];
            //Gets the Virtual Input Column Collection from the Destination Input Collection
            _objIDTSVirtualInput = _objIDTSInput.GetVirtualInput();

            _iDestInputID = Convert.ToInt32(_objIDTSInput.ID);
            // Splits the RAW Column Names into an array of strings
            if (strRAWColNames != null && strRAWColNames.Equals(string.Empty) == false && strRAWColNames != "")
            {
                if (strRAWColNames.EndsWith(","))
                {
                    _iIndex = strRAWColNames.LastIndexOf(_delim);
                    _strIndex = strRAWColNames.Remove(_iIndex);
                }
                _strFilterArray = _strIndex.Split(_delim);
            }
            // Sets Usagetype According to FilterArray
            foreach (IDTSVirtualInputColumn100 objIDTSVirtualInputColumn in _objIDTSVirtualInput.VirtualInputColumnCollection)
            {
                if (_strFilterArray == null)
                {
                    // When FilterArray string is null
                    _objDestinationReaderWrapper.SetUsageType(_iDestInputID, _objIDTSVirtualInput, objIDTSVirtualInputColumn.LineageID, DTSUsageType.UT_READONLY);
                }
                else
                {
                    if (FilterField(objIDTSVirtualInputColumn.Name, _strFilterArray) == false)
                    {
                        // When FilterArray string is not null
                        _objDestinationReaderWrapper.SetUsageType(_iDestInputID, _objIDTSVirtualInput, objIDTSVirtualInputColumn.LineageID, DTSUsageType.UT_READONLY);
                    }
                }
            }
            // Sets the connection
            _objDestinationReaderWrapper.AcquireConnections(null);
            // Reinitializes the Destination Metadata
            _objDestinationReaderWrapper.ReinitializeMetaData();
            // Releases the Wrapper connection
            _objDestinationReaderWrapper.ReleaseConnections();
        }
        
        #endregion
        #region Other Supporting Dataflow Methods

        // Returns true if column name is in unchecked columns array, false otherwise
        internal bool FilterField(string strDestInputColumnName, string[] strArrUncheckedColumns)
        {
            if (strArrUncheckedColumns != null || strArrUncheckedColumns.ToString() != string.Empty)
            {
                foreach (string strUncheckedCol in strArrUncheckedColumns)
                {
                    if (strDestInputColumnName.Equals(strArrUncheckedColumns))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal void ClearRawColumnNames()
        {
            _strRAWColNames.Remove(0, _strRAWColNames.Length);
        }

        #endregion
        #region Save Package
        // Saves a new package to file
        internal string SavePackage()
        {
            // Creates DTS Runtime Application instance
            app = new Microsoft.SqlServer.Dts.Runtime.Wrapper.Application();
            // Save DTSX file to temp folder
            string strPath = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".dtsx";
            app.SaveToXML(strPath, _objPackage, null);
            return strPath;
        }

        #endregion
    #endregion
    }
}
