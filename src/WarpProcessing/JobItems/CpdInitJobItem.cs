﻿using System.Transactions;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Native;
using Warp9.Processing;

namespace Warp9.JobItems
{
    public class CpdInitJobItem : ProjectJobItem
    {
        public CpdInitJobItem(int index, long specTableKey, int baseIndex, string meshColumn, string result) :
            this(index, specTableKey, null, baseIndex, meshColumn, result, null)
        { }

        public CpdInitJobItem(int index, long specTableKey, string? gpaItem, int baseIndex, string meshColumn, string result, CpdConfiguration? cpdCfg) :
            base(index, "CPD initialization", JobItemFlags.FailuesAreFatal | JobItemFlags.RunsAlone)
        {
            SpecimenTableKey = specTableKey;
            GpaItem = gpaItem;
            BaseMeshIndex = baseIndex;
            InitObjectItem = result;
            MeshColumn = meshColumn;

            if (cpdCfg is null)
            {
                CpdConfig = new CpdConfiguration();
                CpdConfig.UseGpu = true;
            }
            else
            {
                CpdConfig = cpdCfg;
            }
        }

        public long SpecimenTableKey { get; init; }
        public string? GpaItem { get; init; }
        public string MeshColumn { get; init; }
        public int BaseMeshIndex { get; init; }
        public string InitObjectItem { get; init; }
        public CpdConfiguration CpdConfig { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            ctx.WriteLog(ItemIndex, MessageKind.Error,
                    string.Format("Initializing CPD for mesh '{0}'.", BaseMeshIndex));

            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                ctx.Project, SpecimenTableKey, MeshColumn);
            if (column is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                    string.Format("Cannot find column '{0}' in entity '{1}'.", MeshColumn, SpecimenTableKey));
                return false;
            }

            if (!ctx.TryGetSpecTableMeshRegistered(SpecimenTableKey, MeshColumn, BaseMeshIndex, GpaItem, out Mesh? baseMesh) || baseMesh is null)
                return false;

            CpdConfiguration cpdCfg = CpdConfig;
            WarpCoreStatus initStat = CpdContext.TryInitNonrigidCpd(out CpdContext? cpdCtx, baseMesh, cpdCfg);
            if (initStat != WarpCoreStatus.WCORE_OK || cpdCtx is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "CPD-LR initialization failed.");
                return false;
            }

            ctx.Workspace.Set(InitObjectItem, cpdCtx);
            ctx.WriteLog(ItemIndex, MessageKind.Information, 
                string.Format("CPD-LR initialization complete (m={0}, eigs={1}).", cpdCtx.NumVertices, cpdCtx.NumEigenvectors));
            return true;
        }
    }
}
