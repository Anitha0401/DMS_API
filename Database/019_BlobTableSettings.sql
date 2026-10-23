/* Where attachment files are kept, one row per blob table (TableName = 'CIR_Blob', 'OtherDocument_Blob').
   Read by DMS.Infrastructure/Features/AttachmentStorage.cs when a file is saved or opened.

   HQ_Storage_Option:
     'Database' - the file is kept in BlobContents on the blob row (also used when a table has no row here)
     'Local'    - a file under HQ_Local_Folder\<TableName>\<BlobID>_<FileName>   (BlobContents stays NULL)
     'Cloud'    - an Azure blob <Cloud_Directory>/<TableName>/<BlobID>_<FileName> in Cloud_ContainerName,
                  using Cloud_AccountName / Cloud_AccountKey                      (BlobContents stays NULL)
   The other columns (Cloud_ContainerName, Cloud_Directory, HQ_*, VSL_*) are copied onto every blob row saved.

   The table is only created when it does not exist yet; an existing BlobTableSettings table is left as it is. */

IF OBJECT_ID('dbo.BlobTableSettings', 'U') IS NULL
BEGIN

    CREATE TABLE [dbo].[BlobTableSettings](
	    [BlobTableSettingsID] [bigint] NOT NULL,
	    [Origin] [varchar](3) NOT NULL,
	    [VesselID] [int] NOT NULL,
	    [TableName] [varchar](150) NULL,
	    [Cloud_AccountName] [varchar](150) NULL,
	    [Cloud_AccountKey] [varchar](500) NULL,
	    [Cloud_ContainerName] [varchar](150) NULL,
	    [Cloud_Directory] [varchar](150) NULL,
	    [HQ_Storage_Option] [varchar](150) NULL,
	    [HQ_Local_Folder] [varchar](150) NULL,
	    [HQ_DatabaseName] [varchar](150) NULL,
	    [VSL_Storage_Option] [varchar](150) NULL,
	    [VSL_DatabaseName] [varchar](150) NULL,
	    [VSL_Local_Folder] [varchar](150) NULL
    ) ON [PRIMARY]
END


insert into [BlobTableSettings] (BlobTableSettingsID,Origin,VesselID,TableName,HQ_Storage_Option,HQ_DatabaseName,VSL_Storage_Option,VSL_DatabaseName)
select (select isnull(max(BlobTableSettingsID),0) from [BlobTableSettings]) + Row_number() Over (Order by o.name)
, 'HQ' Origin, -1 VesselId, o.name as TableName, 'Database' HQ_Storage_Option, '' HQ_DatabaseName, 
'Database' VSL_Storage_Option, '' VSL_DatabaseName
from sys.objects o, sys.columns c
where o.object_id = c.object_Id 
and c.name like '%blobcont%' and type ='u'
and not exists(select * from [BlobTableSettings] where VesselId = -1 and TableName = o.name)

GO

/* Examples (change to suit):

UPDATE dbo.BlobTableSettings
   SET HQ_Storage_Option = 'Cloud',
       Cloud_AccountName = 'mystorageaccount', Cloud_AccountKey = '<key>',
       Cloud_ContainerName = 'dms-attachments', Cloud_Directory = 'HQ'
 WHERE TableName = 'CIR_Blob';

UPDATE dbo.BlobTableSettings
   SET HQ_Storage_Option = 'Local', HQ_Local_Folder = 'D:\DMS\Attachments'
 WHERE TableName = 'OtherDocument_Blob';
*/
