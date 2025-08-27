using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;
using System.Data.SqlTypes;
using System.Reflection.Metadata;

namespace DMS.Data.EF.Handler
{
    public class EditManualCommandHandler : IRequestHandler<EditManualCommand, bool>
    {
        private readonly GeneralQueries _queries;
        DmsContext _dmsContext;

        public EditManualCommandHandler(DmsContext dmsContext, GeneralQueries queries)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _queries = queries;
        }

        public async Task<bool> Handle(EditManualCommand request, CancellationToken cancellationToken)
        {
            string remarks = "";
            var dmManual = _dmsContext.DmManuals
                            .Where(x => x.DM_ManualID == request.Manual.DM_ManualID)
                            .FirstOrDefault();

            if (dmManual == null)
            {
                throw new ArgumentNullException(nameof(dmManual), "Manual not exists");
            }

            var dmManualVersion = _dmsContext.DmManualVersion
                            .Where(x => x.DM_ManualVersionID == request.Manual.DM_ManualVersionID)
                            .FirstOrDefault();

            if (dmManualVersion == null)
            {
                throw new ArgumentNullException(nameof(dmManualVersion), "Manual version not exists");
            }
            if (request.Manual.CalledMode.ToUpper() == "Publish".ToUpper())
            {
                dmManual.DM_StatusID =  _dmsContext.Dm_Status.FirstOrDefault(x => x.StatusName == "To Review")?.DM_StatusID ?? -1;
            }

            if (dmManual.DM_CategoryID != request.Manual.DM_CategoryID)
            {
                var fromCategory = _dmsContext.Dm_Category
                                    .Where(c => c.DM_CategoryID == dmManual.DM_CategoryID)
                                    .Select(x => x.CategoryName)
                                    .FirstOrDefault();

                var toCategory = _dmsContext.Dm_Category
                                  .Where(c => c.DM_CategoryID == request.Manual.DM_CategoryID)
                                  .Select(x => x.CategoryName)
                                  .FirstOrDefault();

                dmManual.DM_CategoryID = request.Manual.DM_CategoryID;

                remarks = remarks +
                    "Category changed from " + fromCategory + " to " + toCategory + Environment.NewLine;
            }

            if (dmManual.ManualCode != request.Manual.ManualCode)
            {
                dmManual.ManualCode = request.Manual.ManualCode;

                remarks = remarks +
                        "Manual code changed from " + dmManual.ManualCode + " to " + request.Manual.ManualCode + Environment.NewLine;
            }

            if (dmManual.ManualName != request.Manual.ManualName)
            {
                dmManual.ManualName = request.Manual.ManualName;

                remarks = remarks +
                        "Manual name changed from " + dmManual.ManualName + " to " + request.Manual.ManualName + Environment.NewLine;
            }

            bool isContentChanged = false;
            if (dmManualVersion.TextContents != request.Manual.TextContents)
            {
                dmManualVersion.TextContents = request.Manual.TextContents;

                    isContentChanged = true;
                    remarks = remarks +
                        "Manual content changed from " + dmManualVersion.TextContents + " to " + request.Manual.TextContents + Environment.NewLine;
            }

            if (isContentChanged)
            {
                var currentVersion = string.IsNullOrEmpty(dmManualVersion.Version) ? "" : dmManualVersion.Version;
                string[] parts = currentVersion.Split('.', StringSplitOptions.RemoveEmptyEntries);
                int major = int.Parse(parts[0]);
                int minor = int.Parse(parts[1]);

                // Next minor version
                string nextMinor = $"{major}.{minor + 1}";

                // Next major version
                string nextVersion = $"{major + 1}.0";

                dmManualVersion.Version = nextVersion;
                remarks = remarks +
                    "Manual version changed from " + currentVersion + " to " + nextVersion + Environment.NewLine;
            }

            if (remarks.Trim().Length > 0)
            {
            dmManual.ModifiedBy = request.Manual.UserID;
            dmManual.ModifiedDate = DateTime.UtcNow;

            _dmsContext.DmManuals.Update(dmManual);
            _dmsContext.DmManualVersion.Update(dmManualVersion);

            // Add Log
                await _queries.AddToLog(request.Manual.DM_ManualID, request.Manual.DM_ManualVersionID, "Manual Edited", "Manual Name: " + request.Manual.ManualName + Environment.NewLine + remarks);
            }

            await _dmsContext.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
