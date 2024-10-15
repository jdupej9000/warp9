using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp9.Data;
using Warp9.Model;
using Warp9.Processing;

namespace Warp9.Jobs
{
    public class LandmarkGpaJobItem : ProjectJobItem
    {
        public LandmarkGpaJobItem(long specTableKey, string colName, string resultKey, GpaConfiguration? cfg) :
            base("Landmark GPA", JobItemFlags.FailuesAreFatal)
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

        protected override bool RunInternal(ProjectJob job)
        {
            SpecimenTableColumn<ProjectReferenceLink>? column = ModelUtils.TryGetSpecimenTableColumn<ProjectReferenceLink>(
                job.Project, SpecimenTableKey, LandmarkColumnName);

            if (column is null)
                return false;

            PointCloud?[] pcls = ModelUtils.LoadSpecimenTableRefs<PointCloud>(job.Project, column).ToArray();
            if (pcls.Any((t) => t is null))
                return false;

            Gpa res = Gpa.Fit(pcls!, Config);
            job.Workspace.Set(WorkspaceResultKey, res);

            return true;
        }
    }
}
