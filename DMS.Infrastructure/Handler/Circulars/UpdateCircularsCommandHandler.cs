using DMS.Data.EF;
using DMS.Data.EF.Context;
using DMS.Data.EF.Models.Circulars;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlTypes;

namespace DMS.Infrastructure.Query.Circulars;

public class UpdateCircularsCommandHandler : IRequestHandler<UpdateCircularsCommand, long>
{
    private readonly CircularsContext _context;

    public UpdateCircularsCommandHandler(CircularsContext context)
    {
        _context = context;
    }

    public async Task<long> Handle(UpdateCircularsCommand request, CancellationToken cancellationToken)
    {
        var dto = request.EditCIR;

        var master = await _context.CIR_Master
            .FirstOrDefaultAsync(m => m.CIR_MasterID == dto.CIR_MasterID, cancellationToken);

        if (master == null)
            throw new InvalidOperationException($"CIR_Master with ID {dto.CIR_MasterID} not found.");

        // Update master fields
        master.CIR_CategoryID = dto.CIR_CategoryID;
        master.CIR_Number = dto.CIR_Number;
        master.Reference = dto.Reference;
        master.Title = dto.Title;
        master.Status = dto.Status;
        master.Priority = dto.Priority;
        master.DateIssued = dto.DateIssued;
        master.ReleasedDate = dto.ReleasedDate;
        master.Remarks = dto.Remarks;
        master.ModifiedBy = dto.CreatedBy ?? master.ModifiedBy;
        master.ModifiedDate = DateTime.UtcNow;

        _context.CIR_Master.Update(master);

        // Update blob if provided
        if (dto.CIR_BlobID != 0)
        {
            try
            {
                var blob = await _context.CIR_Blob
                    .FirstOrDefaultAsync(b => b.CIR_BlobID == dto.CIR_BlobID, cancellationToken);

                if (blob != null)
                {
                    if (dto.BlobContents != null && dto.BlobContents.Length > 0)
                    {
                        blob.BlobContents = dto.BlobContents;
                        blob.BlobSize = dto.BlobSize;
                        blob.FileName = dto.FileName;
                        blob.FileType = dto.FileType;
                        blob.OriginalFileType = dto.OriginalFileType;
                        blob.AddedOn = DateTime.UtcNow;
                    }

                    blob.Description = dto.Description ?? blob.Description;

                    _context.CIR_Blob.Update(blob);
                }
                else if (dto.BlobContents != null && dto.BlobContents.Length > 0)
                {
                    // create new blob entry linked to master
                    var newBlob = new CIR_Blob
                    {
                        CIR_MasterID = dto.CIR_MasterID,
                        Description = dto.Description ?? string.Empty,
                        FileName = dto.FileName,
                        FileType = dto.FileType,
                        OriginalFileType = dto.OriginalFileType,
                        BlobSize = dto.BlobSize,
                        BlobContents = dto.BlobContents,
                        AddedOn = DateTime.UtcNow
                    };

                    await _context.CIR_Blob.AddAsync(newBlob, cancellationToken);
                }
            }
            catch (SqlNullValueException sqlNullEx)
            {
                // Defensive fallback when DB returns a DBNull for a column that the model expects non-null.
                // If client provided new blob contents, create a new blob record instead of failing.
                if (dto.BlobContents != null && dto.BlobContents.Length > 0)
                {
                    var newBlob = new CIR_Blob
                    {
                        CIR_MasterID = dto.CIR_MasterID,
                        Description = dto.Description ?? string.Empty,
                        FileName = dto.FileName,
                        FileType = dto.FileType,
                        OriginalFileType = dto.OriginalFileType,
                        BlobSize = dto.BlobSize,
                        BlobContents = dto.BlobContents,
                        AddedOn = DateTime.UtcNow
                    };

                    await _context.CIR_Blob.AddAsync(newBlob, cancellationToken);
                }
                else
                {
                    // Re-throw with more context so caller can inspect the cause and DB state.
                    throw new InvalidOperationException($"Error reading CIR_Blob with ID {dto.CIR_BlobID}: {sqlNullEx.Message}", sqlNullEx);
                }
            }
        }

        // Remove existing CIR_ToAck_Vsl rows for this master
        var existingToAck = _context.CIR_ToAck_Vsl
            .Where(x => x.CIR_MasterID == dto.CIR_MasterID);

        _context.CIR_ToAck_Vsl.RemoveRange(existingToAck);

        // Insert new rows: one per vessel per rank
        if ((dto.AllowedUserToAck != null && dto.AllowedUserToAck.Count > 0) &&
            (dto.VesselIdList != null && dto.VesselIdList.Count > 0))
        {
            List<string>? userRanksList = (dto.AllowedUserToAck != null && dto.AllowedUserToAck.Any())
                            ? dto.AllowedUserToAck.Distinct().ToList()
                            : new List<string> { string.Empty };

            var vesselIds = (dto.VesselIdList != null && dto.VesselIdList.Any())
                            ? dto.VesselIdList.Distinct().ToList()
                            : new List<int> { -1 };

            var toAckEntities = new List<CIR_ToAck_Vsl>();
            // One block of IDs for every vessel x rank row.
            long idCounter = await _context.NextIdAsync<CIR_ToAck_Vsl>(count: vesselIds.Count * userRanksList.Count, ct: cancellationToken);

            foreach (var vesselId in vesselIds)
            {
                foreach (var rank in userRanksList)
                {
                    toAckEntities.Add(new CIR_ToAck_Vsl
                    {
                        CIR_ToAck_VslID = idCounter++,
                        CIR_MasterID = dto.CIR_MasterID,
                        Origin = "HQ",
                        VesselID = vesselId,
                        UserRank = rank,
                        IsActive = 1
                    });
                }
            }

            if (toAckEntities.Count > 0)
                await _context.CIR_ToAck_Vsl.AddRangeAsync(toAckEntities, cancellationToken);
        }
        // --- End new to-ack handling ---

        await _context.SaveChangesAsync(cancellationToken);

        return master.CIR_MasterID;
    }
}