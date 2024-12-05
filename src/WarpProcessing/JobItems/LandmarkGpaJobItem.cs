﻿using System.Linq;
using Warp9.Data;
using Warp9.Jobs;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.JobItems
{
    /// <summary>
    /// The job item looks into the specimen table with entry index 'specTableKey' in the project,
    /// finds the column 'colName' and requires that column to contain PointClouds. The landmarks/
    /// vertices are collected and registered with GPA (which can be configured with 'Config'). The 
    /// result is a 'Gpa' object that is saved into the job workspace under 'WorkspaceResultKey'.
    /// </summary>
    public class LandmarkGpaJobItem : ProjectJobItem
    {
        public LandmarkGpaJobItem(int index, long specTableKey, string colName, string resultKey, GpaConfiguration? cfg) :
            base(index, "Landmark GPA", JobItemFlags.RunsAlone | JobItemFlags.FailuesAreFatal)
        {
            SpecimenTableKey = specTableKey;
            LandmarkColumnName = colName;
            WorkspaceResultKey = resultKey;

            Config = cfg ?? new GpaConfiguration();
        }

        public long SpecimenTableKey { get; init; }
        public string LandmarkColumnName { get; init; }
        public string WorkspaceResultKey { get; init; }
        public GpaConfiguration Config { get; init; }

        protected override bool RunInternal(IJob job, ProjectJobContext ctx)
        {
            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                ctx.Project, SpecimenTableKey, LandmarkColumnName);

            if (column is null)
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error,
                    string.Format("Cannot find landmark column '{0}' in entity '{1}'.", LandmarkColumnName, SpecimenTableKey));
                return false;
            }

            PointCloud?[] pcls = ModelUtils.LoadSpecimenTableRefs<PointCloud>(ctx.Project, column).ToArray();
            if (pcls.Any((t) => t is null))
            {
                ctx.WriteLog(ItemIndex, MessageKind.Error, "Cannot load landmarks in one or more specimens.");
                return false;
            }

            Gpa res = Gpa.Fit(pcls!, Config);
            ctx.Workspace.Set(WorkspaceResultKey, res);

            ctx.WriteLog(ItemIndex, MessageKind.Information, "GPA complete: " + res.ToString());

            return true;
        }
    }
}