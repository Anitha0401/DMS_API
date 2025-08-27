using AutoMapper;
using DMS.Data.EF.Context;
using DMS.Data.EF.DTO;
using DMS.Data.EF.Models;
using DMS.Data.EF.Query;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DMS.Data.EF.Handler
{
    public class CreateManualCommandHandler : IRequestHandler<CreateManualCommand, CreateManualResultDto>
    {
        private readonly IMapper _mapper;
        private readonly GeneralQueries _queries;
        private readonly DmsContext _dmsContext;

        public CreateManualCommandHandler(DmsContext dmsContext, IMapper mapper, GeneralQueries queries)
        {
            _dmsContext = dmsContext ?? throw new ArgumentNullException(nameof(dmsContext), "DmsContext cannot be null");
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper), "Mapper cannot be null");
            _queries = queries;
        }

        public async Task<CreateManualResultDto> Handle(CreateManualCommand request, CancellationToken cancellationToken)
        {
            var dmManual = _mapper.Map<DM_Manual>(request.Manual);

            var manuals = _dmsContext.DmManuals
                            .AsEnumerable()
                            .Select(i => new
                            {
                                Item = i,
                                ParsedSortOrder = int.TryParse(i.SortOrder, out int val) ? val : 0
                            })
                            .ToList();

            var parentID = request.Manual.CalledManualID;
            if (request.Manual.CalledMode.ToUpper() == "Same Level".ToUpper())
            {
                parentID = manuals
                            .Where(x => x.Item.DM_ManualID == request.Manual.CalledManualID)
                            .Select(x => x.Item.ParentID)
                            .FirstOrDefault();
                if (parentID == 0) parentID = -1;
            }

            #region Sort Order / Addl Sort Order
            var maxSortOrder = 0;
            var parentAddlSortOrder = "";
            if (parentID == -1)
            {
                maxSortOrder = manuals.Max(x => x.ParsedSortOrder) + 1;
            }
            else
            {
                var sortOrder1 = manuals
                                .Where(x => x.Item.ParentID == parentID || x.Item.DM_ManualID == parentID)
                                .Max(x => (int?)x.ParsedSortOrder) ?? 0;
                maxSortOrder = sortOrder1 + 1;

                parentAddlSortOrder = manuals
                                        .Where(x => x.Item.DM_ManualID == parentID)
                                        .Select(x => x.Item.AddlSortOrder)
                                        .FirstOrDefault() ?? string.Empty;
            }

            var sortOrder = maxSortOrder.ToString().PadLeft(6, '0');
            var addlSortOrder = string.IsNullOrEmpty(parentAddlSortOrder)
                                ? sortOrder
                                : $"{parentAddlSortOrder} {sortOrder}";

            var subManualList = manuals
                                .Where(x => x.ParsedSortOrder >= maxSortOrder)
                                .OrderBy(x => x.Item.AddlSortOrder)
                                .ToList();

            foreach (var subManual in subManualList)
            {
                parentAddlSortOrder = subManualList
                                        .Where(x => x.Item.DM_ManualID == subManual.Item.ParentID)
                                        .Select(m => m.Item.AddlSortOrder)
                                        .FirstOrDefault();

                if (parentAddlSortOrder == null)
                {
                    parentAddlSortOrder = manuals
                                            .Where(x => x.Item.DM_ManualID == subManual.Item.ParentID)
                                            .Select(m => m.Item.AddlSortOrder)
                                            .FirstOrDefault();
                }
                subManual.Item.SortOrder = (subManual.ParsedSortOrder + 1).ToString().PadLeft(6, '0');
                subManual.Item.AddlSortOrder = $"{parentAddlSortOrder} {subManual.Item.SortOrder}";
            }
            #endregion

            #region Manual No
            var nextManualNo = "0.0";
            var maxManualNo = await _dmsContext.DmManuals
                                 .Where(x => x.ParentID == parentID)
                                 .OrderByDescending(x => x.ManualNo)
                                 .Select(m => m.ManualNo)
                                 .FirstOrDefaultAsync();
            if (parentID == -1)
            {
                if (decimal.TryParse(maxManualNo, out decimal currentVersion))
                {
                    nextManualNo = (currentVersion + 1).ToString("0.0");
                }
            }
            else if (maxManualNo != null)
            {
                var major = maxManualNo.Substring(0, maxManualNo.LastIndexOf('.'));
                string[] parts = maxManualNo.Split('.');
                int minor = int.Parse(parts[parts.Length - 1]) + 1;
                nextManualNo = $"{major}.{minor.ToString()}";
            }
            else
            {
                maxManualNo = await _dmsContext.DmManuals
                                    .Where(x => x.DM_ManualID == parentID)
                                    .OrderByDescending(x => x.ManualNo)
                                    .Select(m => m.ManualNo)
                                    .FirstOrDefaultAsync();
                nextManualNo = $"{maxManualNo}.1";
            }
            #endregion

            #region Manual Level
            var manualLevel = 0;
            if (parentID != -1)
            {
                var parentManualLevel = await _dmsContext.DmManuals
                                                   .Where(x => x.DM_ManualID == parentID)
                                                   .Select(m => m.ManualLevel)
                                                   .FirstOrDefaultAsync();
                manualLevel = parentManualLevel;
                if (request.Manual.CalledMode.ToUpper() == "Sub Level".ToUpper())
                {
                    manualLevel = parentManualLevel + 1;
                }
            }
            #endregion

            //Insert Manual
            dmManual.DM_ManualID = (_dmsContext.DmManuals
                                 .Select(m => (long?)m.DM_ManualID)
                                 .Max() ?? 0) + 1;
            dmManual.Origin = "HQ";
            dmManual.VesselID = -1;
            dmManual.ManualNo = nextManualNo;
            dmManual.DM_StatusID = _dmsContext.Dm_Status.FirstOrDefault(x => x.StatusName == "NEW")?.DM_StatusID ?? -1;
            dmManual.ParentID = parentID;
            dmManual.ManualLevel = manualLevel;
            dmManual.CanExport = 1;
            dmManual.SortOrder = sortOrder;
            dmManual.AddlSortOrder = addlSortOrder;
            dmManual.CreatedDate = DateTime.UtcNow;
            dmManual.ModifiedBy = null;
            dmManual.ModifiedDate = null;
            dmManual.IsDeleted = 0;

            _dmsContext.DmManuals.Add(dmManual);

            // Insert Manual Version
            var dmManualVersion = _mapper.Map<DM_ManualVersion>(request.Manual);

            dmManualVersion.DM_ManualVersionID = (_dmsContext.DmManualVersion
                                 .Select(m => (long?)m.DM_ManualVersionID)
                                 .Max() ?? 0) + 1;
            dmManualVersion.DM_ManualID = dmManual.DM_ManualID;
            dmManualVersion.Origin = "HQ";
            dmManualVersion.VesselID = -1;
            dmManualVersion.Version = "1.0"; // Default version for new manuals
            dmManualVersion.IsActive = 1;
            dmManualVersion.IsCurrent = 0;
            _dmsContext.DmManualVersion.Add(dmManualVersion);

            await _dmsContext.SaveChangesAsync(cancellationToken);

            await _queries.AddToLog(dmManual.DM_ManualID, dmManualVersion.DM_ManualVersionID, "New Manual Added", "Manual Name: " + dmManual.ManualName);

            return new CreateManualResultDto
            {
                ManualId = dmManual.DM_ManualID,
                ManualVersionId = dmManualVersion.DM_ManualVersionID
            };
        }
    }
}
