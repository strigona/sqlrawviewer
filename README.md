### Project Description

This little program is for opening SSIS 2008+ RAW files. If you're looking for a SSIS 2005 RAW file viewer, head over [here](http://sqlblogcasts.com/blogs/simons/archive/2007/01/11/SSIS-Rawfile-viewer---now-available.aspx).

### SSIS 2016
Available for download

#### Requirements
* SQL Server 2016

### SSIS 2014
Available for download

#### Requirements
* SQL Server 2014

### SSIS 2008 / SSIS 2008 R2
Available for download

#### Requirements
* SQL Server 2008 or SQL Server 2008 R2

### SSIS 2012
I don't have access to SQL Server 2012 so I'm currently unable to provide a 2012 compatible binary. However, with the help of [dnorton](https://www.codeplex.com/site/users/view/dnorton) I've created a [SSIS 2012 branch](https://github.com/strigona/sqlrawviewer/tree/ssis2012). You can download and compile the source.

#### Requirements
* SQL Server 2012

### DLL Dependencies
* Microsoft.SqlServer.Dts.DtsClient.dll
* Microsoft.SqlServer.DTSPipelineWrap.dll
* Microsoft.SqlServer.DTSRuntimeWrap.dll

### Known Issues
* It's a bit of a memory hog. I believe somewhere in the neighbourhood of 7x the RAW file size.
* If the file fails to open via Open With (from Windows Explorer), try opening via RAW Viewer's Open dialog

### To Do
(either as separate programs or the same one)
* Compare memory consumption & functionality between different data viewers(DataGridView, ListView, etc.)
* RAW Diff Viewer - Compare two RAW files to help look for
  * Missing rows
  * Schema differences
  * Different/changed data
* RAW to CSV (export)
* Expose actual data types from RAW file
* Release SSIS 2012 and SSIS 2014 compatible binary
* Release installer
* Check for updates

#### Notes
This project is still very young. It does not handle many exceptions, so if you get any errors let me know and I'll try to patch things up.
Use at your own risk!

#### Special Mentions
This project is based on [Mitulkumar Brahmbhatt](http://social.msdn.microsoft.com/Forums/pl-PL/sqlintegrationservices/thread/e6288076-a23c-4b86-8836-24955434a577)'s code. I ported it from 2005 -> 2008 and added a GUI.
