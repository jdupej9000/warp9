using System;
using System.Collections.Generic;
using System.Text;
using Warp9.Model;

namespace Warp9.Avalonia.Utils
{
    public static class EntitySummary
    {
        public static string SummarizeDca(Project proj, ProjectEntry entry)
        {
            StringBuilder sb = new StringBuilder();

            int numSpecimens = 1;
            SpecimenTable? st = entry.Payload.Table;
            if (st is not null)
            {
                numSpecimens = st.Count;
            }

            MeshCorrespondenceExtraInfo? dca = entry.Payload.MeshCorrExtra;
            if (dca is not null)
            {
                sb.AppendLine("---===[ SOURCE DATASET ]===---");
                sb.AppendLine($"Specimen table      : #{dca.DcaConfig.SpecimenTableKey}");
                sb.AppendLine($"Mesh column         : {dca.DcaConfig.MeshColumnName ?? "(na)"}");
                sb.AppendLine($"Landmark column     : {dca.DcaConfig.LandmarkColumnName ?? "(na)"}");
                sb.AppendLine($"Base specimen index : {dca.DcaConfig.BaseMeshIndex}");
                sb.AppendLine($"Number of specimens : {numSpecimens}");
                sb.AppendLine();

                sb.AppendLine("---===[ DCA CONFIGURATION ]===---");

                sb.AppendLine($"Rigid prealignment  : " + dca.DcaConfig.RigidPreregistration switch
                {
                    Processing.DcaRigidPreregKind.None => "None",
                    Processing.DcaRigidPreregKind.LandmarkFittedGpa => "Landmark-fitted GPA",
                    _ => "(unknown)"
                });

                sb.Append("Nonrigid reg.       : ");
                switch (dca.DcaConfig.NonrigidRegistration)
                {
                    case Processing.DcaNonrigidRegistrationKind.None:
                        sb.AppendLine("None");
                        break;

                    case Processing.DcaNonrigidRegistrationKind.LandmarkFittedTps:
                        sb.AppendLine("Landmark-fitted thin plate spline");
                        break;

                    case Processing.DcaNonrigidRegistrationKind.LowRankCpd:
                        {
                            var cpd = dca.DcaConfig.CpdConfig;

                            sb.AppendLine("Coherent point drift (low-rank)");
                            sb.AppendLine($"   beta             : {cpd.Beta}");
                            sb.AppendLine($"   lambda           : {cpd.Lambda}");
                            sb.AppendLine($"   w                : {cpd.W}");
                            sb.AppendLine($"   tol              : {cpd.Tolerance}");
                            sb.AppendLine($"   max. it.         : {cpd.MaxIterations}");
                            sb.AppendLine($"   initialization   : " + cpd.InitMethod switch
                            {
                                Native.CpdInitMethod.CPD_INIT_EIGENVECTORS => "Eigenvectors (Arnoldi iteration)",
                                Native.CpdInitMethod.CPD_INIT_CLUSTERED => "Sample G and normalize (Nystrom-like)",
                                _ => "(unknown)"
                            });
                        }
                        break;

                    default:
                        sb.AppendLine("unknown");
                        break;
                }

                sb.AppendLine("Projection          : " + dca.DcaConfig.SurfaceProjection switch
                {
                    Processing.DcaSurfaceProjectionKind.None => "None (use nonrigid result)",
                    Processing.DcaSurfaceProjectionKind.ClosestPoint => "Closest point",
                    Processing.DcaSurfaceProjectionKind.RaycastWithFallback => "Raycast, fallback to closest point",
                    _ => "(unknown)"
                });

                List<string> rej = new List<string>();
                if (dca.DcaConfig.RejectDistant)
                    rej.Add($"if distance > {dca.DcaConfig.RejectDistanceThreshold}*IQR");

                if (dca.DcaConfig.RejectExpanded)
                {
                    rej.Add($"if expansion < {dca.DcaConfig.RejectExpandedLowThreshold}");
                    rej.Add($"if expansion > {dca.DcaConfig.RejectExpandedHighThreshold}");
                }

                sb.AppendLine("Match rejection     : " + string.Join(" OR ", rej));

                sb.AppendLine("Match imputation    : " + dca.DcaConfig.RejectImputation switch
                {
                    Processing.DcaImputationKind.None => "None",
                    Processing.DcaImputationKind.Tps => "TPS on good vertices",
                    _ => "(unknown)"
                });

                sb.AppendLine($"Vertex blacklisting : if rejected in {dca.DcaConfig.RejectCountPercent} % of subejcts ({(int)(MathF.Ceiling(dca.DcaConfig.RejectCountPercent / 100 * numSpecimens))})");

                sb.AppendLine("Rigid registration  : " + dca.DcaConfig.RigidPostRegistration switch
                {
                    Processing.DcaRigidPostRegistrationKind.None => "None",
                    Processing.DcaRigidPostRegistrationKind.Gpa => "GPA on all vertices",
                    Processing.DcaRigidPostRegistrationKind.GpaOnWhitelisted => "GPA on whitelisted vertices",
                    _ => "(unknown)"
                });

                sb.AppendLine();               
            }

            sb.AppendLine("---===[ LOG MESSAGES ]===---");
            if (entry.Payload.Text is null)
            {
                sb.AppendLine("(no log has been recorded)");
            }
            else
            {
                sb.AppendLine(entry.Payload.Text);
            }

            return sb.ToString();
        }
    }
}
