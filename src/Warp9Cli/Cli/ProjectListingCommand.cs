using System;

using System.Collections.Generic;
using System.Text;
using Warp9.Model;

namespace Warp9Cli.Cli
{
    public class ProjectListingCommandSpec : ICommandSpec
    {
        public string? Option => "l";

        public string? LongOption => "list";

        public ICommand Parse(CommandTokens tokens)
        {
            return new ProjectListingCommand();
        }
    }

    public class ProjectListingCommand : ICommand
    {
        public void Execute(CommandExecutionContext ctx)
        {
            if (ctx.Project is null)
                throw new InvalidOperationException("No project is loaded.");

            ListEntriesBrief(ctx.Project);
            Console.WriteLine();
            ListReferencesBrief(ctx.Project);
            Console.WriteLine();
            ListSnapshotsBrief(ctx.Project);
            Console.WriteLine();
        }

     
        static void ListEntriesBrief(Project proj)
        {
            Console.WriteLine("Entries:");
            foreach (var kvp in proj.Entries)
            {
                ProjectEntry entry = kvp.Value;
                Console.WriteLine($"  #{kvp.Key} '{entry.Name}' ({entry.Kind})");
                switch (entry.Kind)
                {
                    case ProjectEntryKind.Specimens:
                        ListSpecimenTableBrief(entry);
                        break;

                    case ProjectEntryKind.MeshCorrespondence:
                        ListDcaBrief(entry);
                        break;

                    case ProjectEntryKind.MeshPca:
                        ListMeshPcaBrief(entry);
                        break;

                    case ProjectEntryKind.DiffMatrix:
                        break;
                }
            }
        }

        static void ListSpecimenTableBrief(ProjectEntry entry)
        {
            SpecimenTable? table = entry.Payload.Table;
            if (table is null)
            {
                Console.WriteLine("    [!] specimen table is empty");
            }
            else
            {
                Console.WriteLine($"    {table.Count} specimens, {table.Columns.Count} columns");
            }
        }

        static void ListDcaBrief(ProjectEntry entry)
        {
            MeshCorrespondenceExtraInfo? dca = entry.Payload.MeshCorrExtra;
            if (dca is null)
            {
                Console.WriteLine("    [!] DCA info is not present");
            }
            else
            {
                Console.WriteLine($"    Source data: #{dca.DcaConfig.SpecimenTableKey}:{dca.DcaConfig.MeshColumnName},{dca.DcaConfig.LandmarkColumnName ?? string.Empty}");
            }
        }

        static void ListMeshPcaBrief(ProjectEntry entry)
        {
            PcaExtraInfo? pca = entry.Payload.PcaExtra;
            if (pca is null)
            {
                Console.WriteLine("    [!] PCA info is not present");
            }
            else
            {                
                Console.WriteLine($"    Source data: #{pca.Info.ParentEntityKey}:{pca.Info.ParentColumnName}, size: {pca.Info.ParentSizeColumn ?? string.Empty}");             
            }
        }

        static void ListReferencesBrief(Project proj)
        {
            proj.MakeReferenceStatistics(out int numRefs, out int numExtRefsIncl);
            Console.WriteLine("References:");
            Console.WriteLine($"  {numRefs} total, including {numExtRefsIncl} external");
        }

        static void ListSnapshotsBrief(Project proj)
        {
            Console.WriteLine("Snapshots:");
            Console.WriteLine($"  {proj.Snapshots.Count} total");
        }
    }
}
