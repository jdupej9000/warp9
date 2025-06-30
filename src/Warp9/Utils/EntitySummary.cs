using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using Warp9.Model;

namespace Warp9.Utils
{
    public static class EntitySummary
    {
        public static FlowDocument SummarizeDca(Project proj, ProjectEntry entry)
        {
            FlowDocumentBuilder fdb = new FlowDocumentBuilder();

            int numSpecimens = 1;
            SpecimenTable? st = entry.Payload.Table;
            if (st is not null)
            {
                numSpecimens = st.Count;
            }

            MeshCorrespondenceExtraInfo? dca = entry.Payload.MeshCorrExtra;
            if (dca is not null)
            {
                fdb.AddTitle("Source dataset");

                fdb.StartTable();
                fdb.AddColumn(250);
                fdb.AddColumn(500);

                fdb.StartRow();
                fdb.AddEmphText("Specimen table");
                fdb.AddText($"#{dca.DcaConfig.SpecimenTableKey}");
                fdb.EndRow();

                fdb.StartRow();
                fdb.AddEmphText("Mesh column");
                fdb.AddText(dca.DcaConfig.MeshColumnName ?? "(na)");
                fdb.EndRow();

                fdb.StartRow();
                fdb.AddEmphText("Landmark column");
                fdb.AddText(dca.DcaConfig.LandmarkColumnName ?? "(na)");
                fdb.EndRow();

                fdb.StartRow();
                fdb.AddEmphText("Base specimen index");
                fdb.AddText(dca.DcaConfig.BaseMeshIndex.ToString());
                fdb.EndRow();

                fdb.StartRow();
                fdb.AddEmphText("Number of specimens");
                fdb.AddText(numSpecimens.ToString());
                fdb.EndRow();

                fdb.EndTable();

                fdb.AddTitle("DCA Configuration");

                fdb.StartTable();
                fdb.AddColumn(250);
                fdb.AddColumn(500);

                fdb.StartRow();
                fdb.AddEmphText("Rigid prealignment");
                fdb.AddText(dca.DcaConfig.RigidPreregistration switch
                {
                    Processing.DcaRigidPreregKind.None => "None",
                    Processing.DcaRigidPreregKind.LandmarkFittedGpa => "Landmark-fitted GPA",
                    _ => "(unknown)"
                });
                fdb.EndRow();

                switch (dca.DcaConfig.NonrigidRegistration)
                {
                    case Processing.DcaNonrigidRegistrationKind.None:
                        fdb.StartRow();
                        fdb.AddEmphText("Nonrigid registration");
                        fdb.AddText("None");
                        fdb.EndRow();
                        break;

                    case Processing.DcaNonrigidRegistrationKind.LandmarkFittedTps:
                        fdb.StartRow();
                        fdb.AddEmphText("Nonrigid registration");
                        fdb.AddText("Landmark-fitted thin plate spline");
                        fdb.EndRow();
                        break;

                    case Processing.DcaNonrigidRegistrationKind.LowRankCpd:
                        {
                            var cpd = dca.DcaConfig.CpdConfig;
                            fdb.StartRow();
                            fdb.AddEmphText("Nonrigid registration");
                            fdb.AddText("Coherent point drift (low-rank)");
                            fdb.EndRow();

                            fdb.StartRow();
                            fdb.AddEmphText("CPD config");
                            fdb.AddText($"beta={cpd.Beta}, lambda={cpd.Lambda}, w={cpd.W}, tol={cpd.Tolerance}, max_it={cpd.MaxIterations}");
                            fdb.EndRow();

                            fdb.StartRow();
                            fdb.AddEmphText("CPD initialization");
                            fdb.AddText(cpd.InitMethod switch
                            {
                                Native.CpdInitMethod.CPD_INIT_EIGENVECTORS => "Eigenvectors (Arnoldi iteration)",
                                Native.CpdInitMethod.CPD_INIT_CLUSTERED => "Sample G and normalize (Nystrom-like)",
                                _ => "(unknown)"
                            });
                            fdb.EndRow();
                        }
                        break;

                    default:
                        fdb.StartRow();
                        fdb.AddEmphText("Nonrigid registration");
                        fdb.AddText("(unknown)");
                        fdb.EndRow();
                        break;
                }

                fdb.StartRow();
                fdb.AddEmphText("Projection");
                fdb.AddText(dca.DcaConfig.SurfaceProjection switch
                {
                     Processing.DcaSurfaceProjectionKind.None => "None (use nonrigid result)",
                     Processing.DcaSurfaceProjectionKind.ClosestPoint => "Closest point",
                     Processing.DcaSurfaceProjectionKind.RaycastWithFallback => "Raycast, fallback to closest point",
                     _ => "(unknown)"
                });
                fdb.EndRow();

                fdb.StartRow();
                fdb.AddEmphText("Match rejection");
                List<string> rej = new List<string>();
                if (dca.DcaConfig.RejectDistant)
                    rej.Add($"if distance>{dca.DcaConfig.RejectDistanceThreshold}");

                if (dca.DcaConfig.RejectExpanded)
                {
                    rej.Add($"if expansion<{dca.DcaConfig.RejectExpandedLowThreshold}");
                    rej.Add($"if expansion?{dca.DcaConfig.RejectExpandedHighThreshold}");
                }

                fdb.AddText(string.Join(" OR ", rej));
                fdb.EndRow();

                fdb.StartRow();
                fdb.AddEmphText("Vertex blacklisting");
                fdb.AddText($"if rejected in {dca.DcaConfig.RejectCountPercent} % of subejcts ({(int)(MathF.Ceiling(dca.DcaConfig.RejectCountPercent / 100 * numSpecimens))})");
                fdb.EndRow();

                fdb.StartRow();
                fdb.AddEmphText("Rigid registration");
                fdb.AddText(dca.DcaConfig.RigidPostRegistration switch
                {
                    Processing.DcaRigidPostRegistrationKind.None => "None",
                    Processing.DcaRigidPostRegistrationKind.Gpa => "GPA on all vertices",
                    Processing.DcaRigidPostRegistrationKind.GpaOnWhitelisted => "GPA on whitelisted vertices",
                    _ => "(unknown)"
                });
                fdb.EndRow();

                fdb.EndTable();
            }

            fdb.AddTitle("Log messages");
            if (entry.Payload.Text is null)
            {
                fdb.AddCode("(no log has been recorded)");
            }
            else
            {              
                fdb.AddCode(entry.Payload.Text);
            }

            return fdb.Document;
        }

        
    }
}
